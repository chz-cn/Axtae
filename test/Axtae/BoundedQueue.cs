
using System.Collections.Generic;
using System.Threading.Tasks;
using Axtae;

namespace Test;

#pragma warning disable S2326 // Unused type parameters should be removed
public abstract class BoundedQueueTests<TQueue> where TQueue
  : IBoundedQueue<int> {
#pragma warning restore S2326 // Unused type parameters should be removed
  protected abstract IBoundedQueue<int> CreateQueue(uint capacity);

  [Fact]
  public void Capacity_NotSmallerThan4() {
    var queue = this.CreateQueue(0);
    Assert.Equal(4u, queue.Capacity);
  }

  [Fact]
  public void EnqueueDequeue_OrderIsFIFO() {
    var queue = this.CreateQueue(4);
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
    var queue = this.CreateQueue(4);
    int i = 0;
    while (queue.TryEnqueue(i)) i++;
    Assert.True(i > 0);
    Assert.False(queue.TryEnqueue(i + 1));
    Assert.Equal((uint)i, queue.Count);
  }

  [Fact]
  public void TryDequeue_WhenEmpty_ReturnsFalse() {
    var queue = this.CreateQueue(4);
    Assert.False(queue.TryDequeue(out _));
    Assert.Equal(0u, queue.Count);
  }

  [Fact]
  public void Count_ReflectsItemsCorrectly() {
    var queue = this.CreateQueue(4);
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
    var queue = this.CreateQueue(4);
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
  protected override BoundedMpmcQueue<int> CreateQueue(uint capacity)
    => new(capacity);

  [Fact]
  public async Task Concurrent_MultiProducerMultiConsumer() {
    const int Producers = 4;
    const int Consumers = 4;
    const int ItemsPerProducer = 1000;
    var queue = new BoundedMpmcQueue<int>(128);

    var producerTasks = new List<Task>();
    for (int p = 0; p < Producers; p++) {
      int pid = p;
      producerTasks.Add(Task.Run(async () => {
        for (int i = 0; i < ItemsPerProducer; i++) {
          while (!queue.TryEnqueue((pid * 10000) + i))
            await Task.Yield();
        }
      }));
    }

    const int TotalExpected = Producers * ItemsPerProducer;
    var consumerTasks = new List<Task<List<int>>>();
    for (int c = 0; c < Consumers; c++) {
      consumerTasks.Add(Task.Run(async () => {
        var list = new List<int>();
        while (list.Count < TotalExpected / Consumers) {
          if (queue.TryDequeue(out int v))
            list.Add(v);
          else
            await Task.Yield();
        }
        return list;
      }));
    }

    await Task.WhenAll(producerTasks);
    var results = await Task.WhenAll(consumerTasks);
    int total = 0;
    foreach (var list in results) total += list.Count;
    Assert.Equal(TotalExpected, total);
  }
}

public sealed class MpscQueueTests : BoundedQueueTests<BoundedMpscQueue<int>> {
  protected override BoundedMpscQueue<int> CreateQueue(uint capacity)
    => new(capacity);

  [Fact]
  public async Task Concurrent_MultiProducerSingleConsumer() {
    const int Producers = 4;
    const int ItemsPerProducer = 1000;
    var queue = new BoundedMpscQueue<int>(128);

    var producerTasks = new List<Task>();
    for (int p = 0; p < Producers; p++) {
      int pid = p;
      producerTasks.Add(Task.Run(async () => {
        for (int i = 0; i < ItemsPerProducer; i++) {
          while (!queue.TryEnqueue((pid * 10000) + i))
            await Task.Yield();
        }
      }));
    }

    const int Total = Producers * ItemsPerProducer;
    var results = new List<int>();
    var consumerTask = Task.Run(async () => {
      while (results.Count < Total) {
        if (queue.TryDequeue(out int v))
          results.Add(v);
        else
          await Task.Yield();
      }
    });

    await Task.WhenAll(producerTasks);
    await consumerTask;
    Assert.Equal(Total, results.Count);
  }
}

public sealed class SpmcQueueTests : BoundedQueueTests<BoundedSpmcQueue<int>> {
  protected override BoundedSpmcQueue<int> CreateQueue(uint capacity)
    => new(capacity);

  [Fact]
  public async Task Concurrent_SingleProducerMultiConsumer() {
    const int Consumers = 4;
    const int Items = 4000;
    var queue = new BoundedSpmcQueue<int>(128);

    var producerTask = Task.Run(async () => {
      for (int i = 0; i < Items; i++) {
        while (!queue.TryEnqueue(i))
          await Task.Yield();
      }
    });

    var consumerTasks = new List<Task<List<int>>>();
    for (int c = 0; c < Consumers; c++) {
      consumerTasks.Add(Task.Run(async () => {
        var list = new List<int>();
        while (list.Count < Items / Consumers) {
          if (queue.TryDequeue(out int v))
            list.Add(v);
          else
            await Task.Yield();
        }
        return list;
      }));
    }

    await producerTask;
    var results = await Task.WhenAll(consumerTasks);
    int total = 0;
    foreach (var list in results) total += list.Count;
    Assert.Equal(Items, total);
  }
}

public sealed class SpscQueueTests : BoundedQueueTests<BoundedSpscQueue<int>> {
  protected override BoundedSpscQueue<int> CreateQueue(uint capacity)
    => new(capacity);

  [Fact]
  public async Task Concurrent_SingleProducerSingleConsumer() {
    const int Items = 5000;
    var queue = new BoundedSpscQueue<int>(128);

    var producerTask = Task.Run(async () => {
      for (int i = 0; i < Items; i++) {
        while (!queue.TryEnqueue(i))
          await Task.Yield();
      }
    });

    var results = new List<int>();
    var consumerTask = Task.Run(async () => {
      while (results.Count < Items) {
        if (queue.TryDequeue(out int v))
          results.Add(v);
        else
          await Task.Yield();
      }
    });

    await Task.WhenAll(producerTask, consumerTask);
    Assert.Equal(Items, results.Count);
  }
}
