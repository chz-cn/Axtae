
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Axtae;

namespace Test;

#pragma warning disable S2326 // Unused type parameters should be removed
public abstract class BoundedQueueTests<TQueue> where TQueue
  : IBoundedQueue<int> {
#pragma warning restore S2326 // Unused type parameters should be removed
  protected abstract IBoundedQueue<int> Create(uint capacity);

  [Fact]
  public void Capacity_NotSmallerThan4() {
    var queue = this.Create(0);
    Assert.Equal(4u, queue.Capacity);
  }

  [Fact]
  public void EnqueueDequeue_OrderIsFIFO() {
    var queue = this.Create(4);
    Assert.True(queue.TryEnqueue(1));
    Assert.True(queue.TryEnqueue(2));
    Assert.True(queue.TryEnqueue(3));

    Assert.True(queue.TryDequeue(out int v1));
    Assert.True(queue.TryDequeue(out int v2));
    Assert.True(queue.TryDequeue(out int v3));
    Assert.Equal(1, v1);
    Assert.Equal(2, v2);
    Assert.Equal(3, v3);
  }

  [Fact]
  public void TryEnqueue_WhenFull_ReturnsFalse() {
    var queue = this.Create(4);
    int i = 0;
    while (queue.TryEnqueue(i)) i++;
    Assert.True(i > 0);
    Assert.False(queue.TryEnqueue(i + 1));
    Assert.Equal((uint)i, queue.Count);
  }

  [Fact]
  public void TryDequeue_WhenEmpty_ReturnsFalse() {
    var queue = this.Create(4);
    Assert.False(queue.TryDequeue(out _));
    Assert.Equal(0u, queue.Count);
  }

  [Fact]
  public void Count_ReflectsItemsCorrectly() {
    var queue = this.Create(4);
    Assert.Equal(0u, queue.Count);
    Assert.True(queue.IsEmpty);

    _ = queue.TryEnqueue(5);
    Assert.Equal(1u, queue.Count);
    Assert.False(queue.IsEmpty);

    _ = queue.TryEnqueue(6);
    Assert.Equal(2u, queue.Count);

    _ = queue.TryDequeue(out _);
    Assert.Equal(1u, queue.Count);
  }

  [Fact]
  public void EnqueueDequeue_AfterWraparound_Works() {
    var queue = this.Create(4);
    for (int i = 0; i < 4; i++) Assert.True(queue.TryEnqueue(i));
    for (int i = 0; i < 3; i++) Assert.True(queue.TryDequeue(out _));

    Assert.True(queue.TryEnqueue(100));
    Assert.True(queue.TryDequeue(out int last));
    Assert.Equal(3, last);
    Assert.True(queue.TryDequeue(out last));
    Assert.Equal(100, last);
  }
}

public sealed class MpmcQueueTests : BoundedQueueTests<BoundedMpmcQueue<int>> {
  protected override BoundedMpmcQueue<int> Create(uint capacity)
    => new(capacity);

  [Fact]
  public async Task Concurrent_MultiProducerMultiConsumer() {
    const int Producers = 8;
    const int Consumers = 8;
    const int ItemsPerProducer = 1000;
    var queue = this.Create(128);

    var producers = new InlineArray8<Task>();
    foreach (ref var t in producers)
      t = Task.Run(async () => {
        for (int i = 0; i < ItemsPerProducer; i++)
          while (!queue.TryEnqueue(i))
            await Task.Yield();
      });

    const int TotalExpected = Producers * ItemsPerProducer;

    var consumers = new InlineArray8<Task<int>>();
    foreach (ref var t in consumers)
      t = Task.Run(async () => {
        int count = 0;
        while (count < TotalExpected / Consumers)
          if (queue.TryDequeue(out _))
            count++;
          else
            await Task.Yield();

        return count;
      });

    await Task.WhenAll(producers);
    var results = await Task.WhenAll<int>(consumers);
    int total = 0;
    foreach (var v in results) total += v;
    Assert.Equal(TotalExpected, total);
  }
}

public sealed class MpscQueueTests : BoundedQueueTests<BoundedMpscQueue<int>> {
  protected override BoundedMpscQueue<int> Create(uint capacity)
    => new(capacity);

  [Fact]
  public async Task Concurrent_MultiProducerSingleConsumer() {
    const int Producers = 8;
    const int ItemsPerProducer = 1000;
    var queue = this.Create(128);

    var producers = new InlineArray8<Task>();
    foreach (ref var t in producers)
      t = Task.Run(async () => {
        for (int i = 0; i < ItemsPerProducer; i++)
          while (!queue.TryEnqueue(i))
            await Task.Yield();
      });

    const int Total = Producers * ItemsPerProducer;
    var count = new int();
    var consumer = Task.Run(async () => {
      while (count < Total)
        if (queue.TryDequeue(out _))
          count++;
        else
          await Task.Yield();
    });

    await Task.WhenAll(producers);
    await consumer;
    Assert.Equal(Total, count);
  }
}

public sealed class SpmcQueueTests : BoundedQueueTests<BoundedSpmcQueue<int>> {
  protected override BoundedSpmcQueue<int> Create(uint capacity)
    => new(capacity);

  [Fact]
  public async Task Concurrent_SingleProducerMultiConsumer() {
    const int Consumers = 8;
    const int Items = 4000;
    var queue = this.Create(128);

    var producer = Task.Run(async () => {
      for (int i = 0; i < Items; i++)
        while (!queue.TryEnqueue(i))
          await Task.Yield();
    });

    var consumers = new InlineArray8<Task<int>>();
    foreach (ref var t in consumers)
      t = Task.Run(async () => {
        int count = 0;
        while (count < Items / Consumers)
          if (queue.TryDequeue(out _))
            count++;
          else
            await Task.Yield();

        return count;
      });

    await producer;
    var results = await Task.WhenAll<int>(consumers);
    int total = 0;
    foreach (var v in results) total += v;
    Assert.Equal(Items, total);
  }
}

public sealed class SpscQueueTests : BoundedQueueTests<BoundedSpscQueue<int>> {
  protected override BoundedSpscQueue<int> Create(uint capacity)
    => new(capacity);

  [Fact]
  public async Task Concurrent_SingleProducerSingleConsumer() {
    const int Items = 5000;
    var queue = this.Create(128);

    var producer = Task.Run(async () => {
      for (int i = 0; i < Items; i++)
        while (!queue.TryEnqueue(i))
          await Task.Yield();
    });

    int count = 0;
    var consumer = Task.Run(async () => {
      while (count < Items) {
        if (queue.TryDequeue(out _))
          count++;
        else
          await Task.Yield();
      }
    });

    await Task.WhenAll(producer, consumer);
    Assert.Equal(Items, count);
  }
}
