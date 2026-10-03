// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Axtae;

using Xunit;

using MMQueue = Axtae.BoundedMpmcQueue<int>;
using MSQueue = Axtae.BoundedMpscQueue<int>;
using Queue = Axtae.BoundedQueue<int>;
using SMQueue = Axtae.BoundedSpmcQueue<int>;
using SSQueue = Axtae.BoundedSpscQueue<int>;

namespace Test;

file static class Helper {
  public const byte DefaultCapacity = 16;
  public static (uint, uint) Origin => (0, 0);
}

public abstract class BoundedQueueTests<TQueue>
  where TQueue : struct, IBoundedQueue<int>, allows ref struct {
  protected abstract TQueue Create (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  );

  protected abstract TQueue CreateUninit (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  );

  public TQueue Create (
    Span<int> buffer,
    Span<uint> stamp,
    ref uint head,
    ref uint tail
  ) => this.Create (
    (uint)buffer.Length,
    ref buffer.GetPinnableReference (),
    ref stamp.GetPinnableReference (),
    ref head,
    ref tail
  );

  public TQueue CreateUninit (
    Span<int> buffer,
    Span<uint> stamp,
    ref uint head,
    ref uint tail
  ) => this.CreateUninit (
    (uint)buffer.Length,
    ref buffer.GetPinnableReference (),
    ref stamp.GetPinnableReference (),
    ref head,
    ref tail
  );

  [Fact]
  public void EnqueueDequeue_OrderIsFIFO () {
    const int Cap = Helper.DefaultCapacity;
    Span<int> buffer = stackalloc int[Cap];
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;
    var queue = this.Create (buffer, stamp, ref head, ref tail);

    Assert.True (queue.TryEnqueue (1));
    Assert.True (queue.TryEnqueue (2));
    Assert.True (queue.TryEnqueue (3));
    Assert.True (queue.TryEnqueue (4));

    Assert.True (queue.TryDequeue (out int v1));
    Assert.True (queue.TryDequeue (out int v2));
    Assert.True (queue.TryDequeue (out int v3));
    Assert.True (queue.TryDequeue (out int v4));
    Assert.Equal (1, v1);
    Assert.Equal (2, v2);
    Assert.Equal (3, v3);
    Assert.Equal (4, v4);
  }

  [Fact]
  public void TryEnqueue_WhenFull_ReturnsFalse () {
    const int Cap = Helper.DefaultCapacity;
    Span<int> buffer = stackalloc int[Cap];
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;
    var queue = this.Create (buffer, stamp, ref head, ref tail);

    int i = 0;
    while (queue.TryEnqueue (i)) {
      i++;
    }

    Assert.True (i > 0);
    Assert.False (queue.TryEnqueue (i + 1));
    Assert.Equal ((uint)i, queue.Count);
  }

  [Fact]
  public void TryDequeue_WhenEmpty_ReturnsFalse () {
    const int Cap = Helper.DefaultCapacity;
    Span<int> buffer = stackalloc int[Cap];
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;
    var queue = this.Create (buffer, stamp, ref head, ref tail);

    Assert.False (queue.TryDequeue (out _));
    Assert.Equal (0u, queue.Count);
  }

  [Fact]
  public void Count_TracksItems () {
    const int Cap = Helper.DefaultCapacity;
    Span<int> buffer = stackalloc int[Cap];
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;
    var queue = this.Create (buffer, stamp, ref head, ref tail);

    Assert.Equal (0u, queue.Count);
    Assert.True (queue.IsEmpty);

    _ = queue.TryEnqueue (5);
    Assert.Equal (1u, queue.Count);
    Assert.False (queue.IsEmpty);

    _ = queue.TryEnqueue (6);
    Assert.Equal (2u, queue.Count);

    _ = queue.TryDequeue (out _);
    Assert.Equal (1u, queue.Count);
  }

  [Fact]
  public void Wraparound_PreservesFIFO () {
    const int Cap = Helper.DefaultCapacity;
    Span<int> buffer = stackalloc int[Cap];
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;
    var queue = this.Create (buffer, stamp, ref head, ref tail);

    for (int i = 0; i < buffer.Length; i++) {
      Assert.True (queue.TryEnqueue (i));
    }

    for (int i = 0; i < buffer.Length - 1; i++) {
      Assert.True (queue.TryDequeue (out _));
    }

    Assert.True (queue.TryEnqueue (100));
    Assert.True (queue.TryDequeue (out int last));
    Assert.Equal (Cap - 1, last);
    Assert.True (queue.TryDequeue (out last));
    Assert.Equal (100, last);
  }

  [Theory]
  [InlineData (0u)]
  [InlineData (4u)]
  [InlineData (8u)]
  [InlineData (24u)]
  [InlineData ((1u << 30) + 1)]
  public void Create_RejectsInvalidCapacity (uint capacity)
    => _ = Assert.Throws<ArgumentException> (() => {
      ref int ptr = ref Unsafe.NullRef<int> ();
      ref uint uptr = ref Unsafe.NullRef<uint> ();

      _ = this.Create (capacity, ref ptr, ref uptr, ref uptr, ref uptr);
    });
}

public sealed class MpmcQueueTests : BoundedQueueTests<MMQueue> {
  protected override MMQueue Create (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) {
    MMQueue queue = new (capacity, ref start, ref stamp, ref head, ref tail);
    queue.Initialize ();
    return queue;
  }

  protected override MMQueue CreateUninit (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) => new (capacity, ref start, ref stamp, ref head, ref tail);

  [Fact]
  public static void TryDequeue_ReferenceType_ClearsSlot () {
    const int Cap = Helper.DefaultCapacity;
    var data = new InlineArray16<string> ();
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;

    var queue = new BoundedMpmcQueue<string>
      (Cap, ref data[0], ref stamp[0], ref head, ref tail);
    queue.Initialize ();

    Assert.True (queue.TryEnqueue ("hello"));
    Assert.Equal ("hello", data[0]);

    Assert.True (queue.TryDequeue (out string? v));
    Assert.Equal ("hello", v);
    Assert.Null (data[0]);

    Assert.True (queue.TryEnqueue ("world"));
    Assert.True (queue.TryDequeue (out v));
    Assert.Equal ("world", v);
    Assert.Null (data[0]);
  }

  [Fact]
  public void Fresh_RebuildsStampFromHeadTail () {
    const int Cap = Helper.DefaultCapacity;
    Span<int> data = stackalloc int[Cap];
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;
    var q1 = this.Create (data, stamp, ref head, ref tail);

    for (int i = 0; i < 8; i++) {
      Assert.True (q1.TryEnqueue (i));
    }

    for (int i = 0; i < 3; i++) {
      Assert.True (q1.TryDequeue (out _));
    }

    stamp.Clear ();

    var q2 = this.CreateUninit (data, stamp, ref head, ref tail);
    q2.Fresh ();

    Assert.Equal (5u, q2.Count);
    for (int i = 3; i < 8; i++) {
      Assert.True (q2.TryDequeue (out int v));
      Assert.Equal (i, v);
    }

    Assert.True (q2.IsEmpty);

    for (int i = 0; i < Cap; i++) {
      Assert.True (q2.TryEnqueue (i + 100));
    }

    for (int i = 0; i < Cap; i++) {
      Assert.True (q2.TryDequeue (out int v));
      Assert.Equal (i + 100, v);
    }
  }

  [Fact]
  public async Task Concurrent_MultiProducerMultiConsumer () {
    const int Producers = 2;
    const int Consumers = 2;
    const int ItemsPerProducer = 500;
    const int Cap = 32;
    var owner = new Owner (this, Cap, Producers + Consumers);
    owner.Initialize ();

    var producers = new InlineArray2<Task> ();
    foreach (ref var t in producers) {
      t = Task.Factory.StartNew (
        Producer,
        owner,
        TestContext.Current.CancellationToken,
        TaskCreationOptions.RunContinuationsAsynchronously,
        TaskScheduler.Default
      );

      static void Producer (object? obj) {
        var queue = ((Owner)obj!).Queue ();
        int i = 0;
        while (i < ItemsPerProducer) {
          if (queue.TryEnqueue (i)) {
            i++;
          }
        }
      }
    }

    const int TotalExpected = Producers * ItemsPerProducer;

    var consumers = new InlineArray2<Task<int>> ();
    foreach (ref var t in consumers) {
      t = Task.Factory.StartNew (
        Consumer,
        owner,
        TestContext.Current.CancellationToken,
        TaskCreationOptions.RunContinuationsAsynchronously,
        TaskScheduler.Default
      );

      static int Consumer (object? obj) {
        var queue = ((Owner)obj!).Queue ();
        int count = 0;
        while (count < TotalExpected / Consumers) {
          if (queue.TryDequeue (out _)) {
            count++;
          }
        }

        return count;
      }
    }

    await Task.WhenAll (producers);
    int[] results = await Task.WhenAll<int> (consumers);
    int total = 0;
    foreach (int v in results) {
      total += v;
    }

    Assert.Equal (TotalExpected, total);
  }
}

[StructLayout (LayoutKind.Explicit)]
file sealed class Owner (MpmcQueueTests self, uint capacity, uint count) {
  [FieldOffset (0)] private uint _head = 0;
  [FieldOffset (8)] private readonly int[] _data = new int[capacity];
  [FieldOffset (16)] private readonly uint[] _stamp = new uint[capacity];
  [FieldOffset (24)] private readonly MpmcQueueTests _self = self;
  [FieldOffset (32)] private uint _count = count;
  [FieldOffset (64)] private uint _tail = 0;

  public void Initialize () => _ = this._self.Create
    (this._data, this._stamp, ref this._head, ref this._tail);

  public MMQueue Queue () {
    uint i = Interlocked.Decrement (ref this._count);
    while (i > 0) {
      Thread.SpinWait (10);
      i = Volatile.Read (ref this._count);
    }

    return this._self.CreateUninit
      (this._data, this._stamp, ref this._head, ref this._tail);
  }
}

public sealed class MpscQueueTests : BoundedQueueTests<MSQueue> {
  protected override MSQueue Create (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) {
    MSQueue queue = new (capacity, ref start, ref stamp, ref head, ref tail);
    queue.Initialize ();
    return queue;
  }

  protected override MSQueue CreateUninit (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) => new (capacity, ref start, ref stamp, ref head, ref tail);

  [Fact]
  public static void TryDequeue_ReferenceType_ClearsSlot () {
    const int Cap = Helper.DefaultCapacity;
    var data = new InlineArray16<string> ();
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;

    var queue = new BoundedMpscQueue<string>
      (Cap, ref data[0], ref stamp[0], ref head, ref tail);
    queue.Initialize ();

    Assert.True (queue.TryEnqueue ("hello"));
    Assert.Equal ("hello", data[0]);

    Assert.True (queue.TryDequeue (out string? v));
    Assert.Equal ("hello", v);
    Assert.Null (data[0]);

    Assert.True (queue.TryEnqueue ("world"));
    Assert.True (queue.TryDequeue (out v));
    Assert.Equal ("world", v);
    Assert.Null (data[0]);
  }

  [Fact]
  public void Fresh_RebuildsStampFromHeadTail () {
    const int Cap = Helper.DefaultCapacity;
    Span<int> data = stackalloc int[Cap];
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;
    var q1 = this.Create (data, stamp, ref head, ref tail);

    for (int i = 0; i < 8; i++) {
      Assert.True (q1.TryEnqueue (i));
    }

    for (int i = 0; i < 3; i++) {
      Assert.True (q1.TryDequeue (out _));
    }

    stamp.Clear ();

    var q2 = this.CreateUninit (data, stamp, ref head, ref tail);
    q2.Fresh ();

    Assert.Equal (5u, q2.Count);
    for (int i = 3; i < 8; i++) {
      Assert.True (q2.TryDequeue (out int v));
      Assert.Equal (i, v);
    }

    Assert.True (q2.IsEmpty);

    for (int i = 0; i < Cap; i++) {
      Assert.True (q2.TryEnqueue (i + 100));
    }

    for (int i = 0; i < Cap; i++) {
      Assert.True (q2.TryDequeue (out int v));
      Assert.Equal (i + 100, v);
    }
  }
}

public sealed class SpmcQueueTests : BoundedQueueTests<SMQueue> {
  protected override SMQueue Create (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) {
    SMQueue queue = new (capacity, ref start, ref stamp, ref head, ref tail);
    queue.Initialize ();
    return queue;
  }

  protected override SMQueue CreateUninit (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) => new (capacity, ref start, ref stamp, ref head, ref tail);

  [Fact]
  public static void TryDequeue_ReferenceType_ClearsSlot () {
    const int Cap = Helper.DefaultCapacity;
    var data = new InlineArray16<string> ();
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;

    var queue = new BoundedSpmcQueue<string>
      (Cap, ref data[0], ref stamp[0], ref head, ref tail);
    queue.Initialize ();

    Assert.True (queue.TryEnqueue ("hello"));
    Assert.Equal ("hello", data[0]);

    Assert.True (queue.TryDequeue (out string? v));
    Assert.Equal ("hello", v);
    Assert.Null (data[0]);

    Assert.True (queue.TryEnqueue ("world"));
    Assert.True (queue.TryDequeue (out v));
    Assert.Equal ("world", v);
    Assert.Null (data[0]);
  }

  [Fact]
  public void Fresh_RebuildsStampFromHeadTail () {
    const int Cap = Helper.DefaultCapacity;
    Span<int> data = stackalloc int[Cap];
    Span<uint> stamp = stackalloc uint[Cap];
    var (head, tail) = Helper.Origin;
    var q1 = this.Create (data, stamp, ref head, ref tail);

    for (int i = 0; i < 8; i++) {
      Assert.True (q1.TryEnqueue (i));
    }

    for (int i = 0; i < 3; i++) {
      Assert.True (q1.TryDequeue (out _));
    }

    stamp.Clear ();

    var q2 = this.CreateUninit (data, stamp, ref head, ref tail);
    q2.Fresh ();

    Assert.Equal (5u, q2.Count);
    for (int i = 3; i < 8; i++) {
      Assert.True (q2.TryDequeue (out int v));
      Assert.Equal (i, v);
    }

    Assert.True (q2.IsEmpty);

    for (int i = 0; i < Cap; i++) {
      Assert.True (q2.TryEnqueue (i + 100));
    }

    for (int i = 0; i < Cap; i++) {
      Assert.True (q2.TryDequeue (out int v));
      Assert.Equal (i + 100, v);
    }
  }
}

public sealed class SpscQueueTests : BoundedQueueTests<SSQueue> {
  protected override SSQueue Create (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) {
    SSQueue queue = new (capacity, ref start, ref head, ref tail);
    queue.Initialize ();
    return queue;
  }

  protected override SSQueue CreateUninit (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) => new (capacity, ref start, ref head, ref tail);

  [Fact]
  public static void TryDequeue_ReferenceType_ClearsSlot () {
    const int Cap = Helper.DefaultCapacity;
    var data = new InlineArray16<string> ();
    var (head, tail) = Helper.Origin;

    var queue = new BoundedSpscQueue<string>
      (Cap, ref data[0], ref head, ref tail);
    queue.Initialize ();

    Assert.True (queue.TryEnqueue ("hello"));
    Assert.Equal ("hello", data[0]);

    Assert.True (queue.TryDequeue (out string? v));
    Assert.Equal ("hello", v);
    Assert.Null (data[0]);

    Assert.True (queue.TryEnqueue ("world"));
    Assert.True (queue.TryDequeue (out v));
    Assert.Equal ("world", v);
    Assert.Null (data[0]);
  }
}

public sealed class QueueTests : BoundedQueueTests<Queue> {
  protected override Queue Create (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) {
    Queue queue = new (capacity, ref start, ref head, ref tail);
    queue.Initialize ();
    return queue;
  }

  protected override Queue CreateUninit (
    uint capacity,
    ref int start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) => new (capacity, ref start, ref head, ref tail);

  [Fact]
  public static void TryDequeue_ReferenceType_ClearsSlot () {
    const int Cap = Helper.DefaultCapacity;
    var data = new InlineArray16<string> ();
    var (head, tail) = Helper.Origin;

    var queue = new BoundedQueue<string>
      (Cap, ref data[0], ref head, ref tail);
    queue.Initialize ();

    Assert.True (queue.TryEnqueue ("hello"));
    Assert.Equal ("hello", data[0]);

    Assert.True (queue.TryDequeue (out string? v));
    Assert.Equal ("hello", v);
    Assert.Null (data[0]);

    Assert.True (queue.TryEnqueue ("world"));
    Assert.True (queue.TryDequeue (out v));
    Assert.Equal ("world", v);
    Assert.Null (data[0]);
  }
}
