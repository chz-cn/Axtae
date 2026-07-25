
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Axtae;

using CloseExp = System.Threading.Channels.ChannelClosedException;

namespace Test;

public abstract class BoundedChannelTests<TChannel> where TChannel
  : IChannel<int> {
  protected abstract TChannel Create(uint capacity);

  [Fact]
  public void Capacity_NotSmallerThan4() {
    var queue = this.Create(0);
    Assert.Equal(4u, queue.Capacity);
  }

  [Fact]
  public async Task WriteRead_OrderIsFIFO() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    var reader = channel.Reader;

    Assert.True(writer.TryWrite(1));
    Assert.True(writer.TryWrite(2));
    Assert.True(writer.TryWrite(3));

    Assert.Equal(1, await reader.ReadAsync());
    Assert.Equal(2, await reader.ReadAsync());
    Assert.Equal(3, await reader.ReadAsync());
  }

  [Fact]
  public void TryWrite_WhenFull_ReturnsFalse() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    int i = 0;
    while (writer.TryWrite(i)) i++;
    Assert.True(i > 0);
    Assert.False(writer.TryWrite(i + 1));
  }

  [Fact]
  public async Task TryWrite_WhenCompleting_ReturnsFalse() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    await writer.WriteAsync(1);
    writer.Complete();
    Assert.Equal(Channel.Completing, channel.State);

    Assert.False(writer.TryWrite(2));

    await writer.WriteAsync(3);
    var reader = channel.Reader;
    var read = await reader.ReadAsync();
    Assert.Equal(1, read);
    Assert.False(reader.TryRead(out _));
  }

  [Fact]
  public async Task WriteAsync_AfterCompleted_IsIgnored() {
    var channel = this.Create(1);
    var writer = channel.Writer;
    var reader = channel.Reader;

    await writer.WriteAsync(1);
    writer.Complete();
    _ = await reader.ReadAsync();
    await reader.Completion;

    await writer.WriteAsync(999);
    Assert.False(reader.TryRead(out _));
  }

  [Fact]
  public async Task WriteAsync_WhenFull_BlocksUntilSpaceAvailable() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    var reader = channel.Reader;

    int i = 0;
    while (writer.TryWrite(i)) i++;

    var write_task = writer.WriteAsync(999).AsTask();

    var completed = await Task.WhenAny(write_task, Task.Delay(50));
    Assert.NotSame(write_task, completed);

    _ = await reader.ReadAsync();
    await write_task;

    bool found999 = false;
    while (!found999) {
      int val = await reader.ReadAsync();
      if (val is 999) found999 = true;
    }
    Assert.True(found999);
  }

  [Fact]
  public async Task TryRead_AfterCompleted_ReturnsFalse() {
    var channel = this.Create(1);
    var writer = channel.Writer;
    var reader = channel.Reader;

    await writer.WriteAsync(1);
    writer.Complete();
    _ = await reader.ReadAsync();
    await reader.Completion;

    Assert.False(reader.TryRead(out _));
  }

  [Fact]
  public void TryRead_WhenEmpty_ReturnsFalse() {
    var channel = this.Create(4);
    var reader = channel.Reader;

    Assert.False(reader.TryRead(out _));
  }

  [Fact]
  public void TryRead_WhenActive_ReturnsTrue() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    var reader = channel.Reader;

    Assert.True(writer.TryWrite(42));
    Assert.True(reader.TryRead(out int item));
    Assert.Equal(42, item);

    Assert.Equal(Channel.Active, channel.State);
  }

  [Fact]
  public async Task ReadAsync_WhenEmpty_BlocksUntilItemAvailable() {
    var channel = this.Create(1);
    var writer = channel.Writer;
    var reader = channel.Reader;

    var read_task = reader.ReadAsync().AsTask();
    var completed = await Task.WhenAny(read_task, Task.Delay(100));
    Assert.NotSame(read_task, completed);

    await writer.WriteAsync(99);
    Assert.Equal(99, await read_task);
  }

  [Fact]
  public async Task ReadAsync_AfterCompleted_ThrowsChannelClosedException() {
    var channel = this.Create(1);
    var writer = channel.Writer;
    var reader = channel.Reader;

    await writer.WriteAsync(1);
    writer.Complete();

    _ = await reader.ReadAsync();
    await reader.Completion;

    _ = await Assert.ThrowsAsync<CloseExp>(() => reader.ReadAsync().AsTask());
  }

  [Fact]
  public async Task Complete_DrainMode_DrainsRemainingItems() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    var reader = channel.Reader;

    await writer.WriteAsync(1);
    await writer.WriteAsync(2);
    writer.Complete();

    List<int> list = new(2);
    while (reader.TryRead(out int item))
      list.Add(item);

    await reader.Completion;
    Assert.Equal([1, 2], list);
  }

  [Fact]
  public async Task Complete_RejectMode_RejectsNewWrites() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    var reader = channel.Reader;

    await writer.WriteAsync(1);
    writer.Complete();
    Assert.NotEqual(Channel.Active, channel.State);

    Assert.False(writer.TryWrite(100));

    await writer.WriteAsync(200);

    int remaining = await reader.ReadAsync();
    Assert.Equal(1, remaining);

    await reader.Completion;
    Assert.Equal(Channel.Completed, channel.State);
  }

  [Fact]
  public void Complete_Idempotent() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    writer.Complete();
    writer.Complete();
    Assert.Equal(Channel.Completed, channel.State);
  }

  [Fact]
  public async Task Complete_ConcurrentCalls_OnlyFirstSucceeds() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    await writer.WriteAsync(1);

    var tasks = new InlineArray4<Task>();

    foreach (ref var task in tasks)
      task = Task.Run(writer.Complete);

    await Task.WhenAll(tasks);

    Assert.Equal(Channel.Completing, channel.State);

    _ = await channel.Reader.ReadAsync();
    await channel.Reader.Completion;
    Assert.Equal(Channel.Completed, channel.State);
  }

  [Fact]
  public async Task ConcurrentStressTest() {
    const int Producers = 4;
    const int ItemsPerProducer = 500;
    var channel = this.Create(32);
    var writer = channel.Writer;
    var reader = channel.Reader;

    var producer_tasks = new Task[Producers];
    for (int p = 0; p < Producers; p++) {
      int pid = p;
      producer_tasks[p] = Task.Run(async () => {
        for (int i = 0; i < ItemsPerProducer; i++)
          await writer.WriteAsync((pid * 10000) + i);
      });
    }

    var results = new System.Collections.Concurrent.ConcurrentBag<int>();
    var consumer_task = Task.Run(async () => {
      int count = 0;
      const int Total = Producers * ItemsPerProducer;
      while (count < Total) {
        int item = await reader.ReadAsync();
        results.Add(item);
        count++;
      }
      await reader.Completion;
    });

    await Task.WhenAll(producer_tasks);
    writer.Complete();
    await consumer_task;

    Assert.Equal(Producers * ItemsPerProducer, results.Count);
  }
}

public sealed class DrainChannelTests
  : BoundedChannelTests<DrainBoundedChannel<int>> {
  protected override DrainBoundedChannel<int> Create(uint capacity) =>
    (DrainBoundedChannel<int>)Channel.CreateBounded<int>(capacity, false);

  [Fact]
  public async Task ReadAsync_WhenCompleted_DoesCancel() {
    var channel = this.Create(4);
    var reader = channel.Reader;
    var writer = channel.Writer;

    var task = Task.Run(async () => await reader.ReadAsync());

    await Task.Delay(1);
    writer.Complete();

    _ = await Assert.ThrowsAsync<CloseExp>(() => task);
  }
}

public sealed class RejectChannelTests
  : BoundedChannelTests<RejectBoundedChannel<int>> {
  protected override RejectBoundedChannel<int> Create(uint capacity) =>
    (RejectBoundedChannel<int>)Channel.CreateBounded<int>(capacity, true);

  [Fact]
  public async Task TryWrite_DoubleCheck_ConcurrentRace() {
    const int Iterations = 1000;
    var tasks = new InlineArray11<Task>();

    for (int attempt = 0; attempt < Iterations; attempt++) {
      var channel = this.Create(8);
      var writer = channel.Writer;

      foreach (ref var task in tasks[..10])
        task = Task.Run(() => writer.TryWrite(100));

      tasks[10] = Task.Run(writer.Complete);

      await Task.WhenAll(tasks);
    }

    Assert.True(true);
  }

  [Fact]
  public async Task WriteAsync_WhenFull_CancelledByComplete() {
    var channel = this.Create(4);
    var writer = channel.Writer;
    var reader = channel.Reader;

    for (int i = 0; i < 4; i++) _ = writer.TryWrite(i);

    var writeTask = writer.WriteAsync(999).AsTask();

    await Task.Delay(50);
    Assert.False(writeTask.IsCompleted);

    writer.Complete();

    await writeTask;
    var remaining = new List<int>(4);
    while (reader.TryRead(out int v)) remaining.Add(v);
    Assert.DoesNotContain(999, remaining);

    await reader.Completion;
    Assert.Equal(Channel.Completed, channel.State);
  }

  [Fact]
  public async Task WriteAsync_DoubleCheck_ConcurrentRace() {
    const int Iterations = 1000;
    var tasks = new InlineArray11<Task>();

    for (int attempt = 0; attempt < Iterations; attempt++) {
      var channel = this.Create(8);
      var writer = channel.Writer;

      foreach (ref var task in tasks[..10])
        task = Task.Run(() => writer.WriteAsync(100));

      tasks[10] = Task.Run(writer.Complete);

      await Task.WhenAll(tasks);
    }

    Assert.True(true);
  }

  [Fact]
  public async Task ReadAsync_WhenCompleted_DoesCancel() {
    var channel = this.Create(4);
    var reader = channel.Reader;
    var writer = channel.Writer;

    var task = Task.Run(async () => await reader.ReadAsync());

    await Task.Delay(1);
    writer.Complete();

    _ = await Assert.ThrowsAsync<CloseExp>(() => task);
  }
}
