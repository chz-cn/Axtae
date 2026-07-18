
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;

namespace Test;

public abstract class BoundedChannelTests<TChannel> where TChannel
  : IBoundedChannel<int> {
  protected abstract TChannel CreateChannel(uint capacity);

  [Fact]
  public void Capacity_NotSmallerThan4() {
    var queue = this.CreateChannel(0);
    Assert.Equal(4u, queue.Capacity);
  }

  [Fact]
  public async Task WriteRead_OrderIsFIFO() {
    var channel = this.CreateChannel(4);
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
    var channel = this.CreateChannel(4);
    var writer = channel.Writer;
    int i = 0;
    while (writer.TryWrite(i)) i++;
    Assert.True(i > 0);
    Assert.False(writer.TryWrite(i + 1));
  }

  [Fact]
  public void TryRead_WhenEmpty_ReturnsFalse() {
    var channel = this.CreateChannel(4);
    var reader = channel.Reader;

    Assert.False(reader.TryRead(out _));
  }

  [Fact]
  public async Task WriteAsync_WhenFull_BlocksUntilSpaceAvailable() {
    var channel = this.CreateChannel(4);
    var writer = channel.Writer;
    var reader = channel.Reader;

    int i = 0;
    while (writer.TryWrite(i)) i++;

    var writeTask = writer.WriteAsync(999);
    await Task.Delay(50);
    Assert.False(writeTask.IsCompleted);

    _ = await reader.ReadAsync();
    await writeTask;

    bool found999 = false;
    while (!found999) {
      int val = await reader.ReadAsync();
      if (val is 999) found999 = true;
    }
    Assert.True(found999);
  }

  [Fact]
  public async Task ReadAsync_WhenEmpty_BlocksUntilItemAvailable() {
    var channel = this.CreateChannel(1);
    var writer = channel.Writer;
    var reader = channel.Reader;

    var readTask = reader.ReadAsync().AsTask();
    await Task.Delay(100);
    Assert.False(readTask.IsCompleted);

    await writer.WriteAsync(99);
    Assert.Equal(99, await readTask);
  }

  [Fact]
  public async Task Complete_DrainMode_DrainsRemainingItems() {
    var channel = this.CreateChannel(4);
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
    var channel = this.CreateChannel(4);
    var writer = channel.Writer;
    var reader = channel.Reader;

    await writer.WriteAsync(1);
    writer.Complete();
    Assert.NotEqual(Channel.Active, channel.State);

    Assert.False(writer.TryWrite(100));

    int remaining = await reader.ReadAsync();
    Assert.Equal(1, remaining);
  }

  [Fact]
  public async Task ConcurrentStressTest() {
    const int Producers = 4;
    const int ItemsPerProducer = 500;
    var channel = this.CreateChannel(32);
    var writer = channel.Writer;
    var reader = channel.Reader;

    var producerTasks = new Task[Producers];
    for (int p = 0; p < Producers; p++) {
      int pid = p;
      producerTasks[p] = Task.Run(async () => {
        for (int i = 0; i < ItemsPerProducer; i++)
          await writer.WriteAsync((pid * 10000) + i);
      });
    }

    var results = new System.Collections.Concurrent.ConcurrentBag<int>();
    var consumerTask = Task.Run(async () => {
      int count = 0;
      const int Total = Producers * ItemsPerProducer;
      while (count < Total)
        if (reader.TryRead(out int v)) {
          results.Add(v);
          count++;
        }
        else
          await Task.Delay(1);

      await reader.Completion;
    });

    await Task.WhenAll(producerTasks);
    writer.Complete();
    await consumerTask;

    Assert.Equal(Producers * ItemsPerProducer, results.Count);
  }
}

public sealed class DrainChannelTests : BoundedChannelTests<DrainBoundedChannel<int>> {
  protected override DrainBoundedChannel<int> CreateChannel(uint capacity) =>
    (DrainBoundedChannel<int>)Channel.CreateBounded<int>(capacity, false);
}

public sealed class RejectChannelTests : BoundedChannelTests<RejectBoundedChannel<int>> {
  protected override RejectBoundedChannel<int> CreateChannel(uint capacity) =>
    (RejectBoundedChannel<int>)Channel.CreateBounded<int>(capacity, true);
}
