// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;

namespace Axtae;

#pragma warning disable CA1711 // 标识符应采用正确的后缀

/// <summary>
/// Represents a bounded FIFO queue with non-blocking operations.
/// </summary>
/// <typeparam name="T">The type of items stored in the queue.</typeparam>
public interface IBoundedQueue<T> {
  /// <summary>
  /// Gets the maximum number of items the queue can hold.
  /// </summary>
  uint Capacity { get; }

  /// <summary>
  /// Gets the current number of items in the queue.
  /// </summary>
  /// <remarks>
  /// This value is an instantaneous snapshot of the number of items in the
  /// queue at the moment of the call.
  /// It may become stale immediately due to concurrent enqueue or dequeue
  /// operations.
  /// Do not use this value to determine whether the queue is empty or full.
  /// </remarks>
  uint Count { get; }

  /// <summary>
  /// Gets a value indicating whether the queue is empty.
  /// </summary>
  /// <value>
  /// <see langword="true"/> if the queue contains no items;
  /// otherwise, <see langword="false"/>.
  /// </value>
  bool IsEmpty { get; }

  /// <summary>
  /// Attempts to add an item to the queue.
  /// </summary>
  /// <param name="item">The item to enqueue.</param>
  /// <returns>
  /// <see langword="true"/> if the item was successfully enqueued;
  /// <see langword="false"/> if the queue is full.
  /// </returns>
  bool TryEnqueue (T item);

  /// <summary>
  /// Attempts to dequeue an item without blocking.
  /// </summary>
  /// <param name="item">
  /// When successful, contains the dequeued item;
  /// otherwise, the <see langword="default"/> value.
  /// </param>
  /// <returns>
  /// <see langword="true"/> if an item was successfully dequeued;
  /// <see langword="false"/> if the queue is empty.
  /// </returns>
  bool TryDequeue ([MaybeNullWhen (false)] out T item);
}

/// <summary>
/// A bounded multi-producer / multi-consumer (MPMC) queue.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// <para>
/// This queue is thread-safe and lock-free.
/// </para>
/// <para>
/// The queue does not own its storage. The caller supplies the backing
/// storage via <c>ref</c> parameters and is responsible for keeping it alive
/// for the lifetime of this instance.
/// </para>
/// <para>
/// The <c>head</c> and <c>tail</c> positions must be separated by at least 60
/// bytes so that they never share a cache line. Placing them 64 bytes apart
/// (or in separate cache-line-aligned fields) is recommended; otherwise
/// producers and consumers will contend on the same line.
/// </para>
/// <para>
/// The <c>stamp</c> array must be initialized before use by calling
/// <see cref="Initialize"/> or <see cref="Fresh"/>.
/// </para>
/// </remarks>
public readonly ref struct BoundedMpmcQueue<T> : IBoundedQueue<T> {
  private readonly ref T _start;
  private readonly ref uint _stamp;
  private readonly ref uint _head;
  private readonly ref uint _tail;
  private readonly int _shift;

  /// <inheritdoc />
  public readonly uint Capacity { get; }

  /// <inheritdoc />
  public uint Count {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => Volatile.Read (ref this._tail) - Volatile.Read (ref this._head);
  }

  /// <inheritdoc />
  public bool IsEmpty {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => this.Count is 0;
  }

  /// <summary>
  /// Initializes a new <see cref="BoundedMpmcQueue{T}"/> view over
  /// caller-supplied storage.
  /// </summary>
  /// <param name="capacity">
  /// The queue capacity. Must be a power of two in <c>[16, 2^30]</c>.
  /// </param>
  /// <param name="start">
  /// A reference to the first item slot.
  /// Must span at least <c>capacity</c> elements.
  /// </param>
  /// <param name="stamp">
  /// A reference to the stamp array. Must span at least <c>capacity</c>
  /// elements. The mapping from logical index to physical offset is
  /// interleaved; see <see cref="Initialize"/> for details.
  /// </param>
  /// <param name="head">
  /// A reference to the head position. Must be separated from
  /// <paramref name="tail"/> by at least 60 bytes; see the type remarks.
  /// </param>
  /// <param name="tail">
  /// A reference to the tail position. Must satisfy
  /// <c>unchecked(tail - head) &lt;= capacity</c>.
  /// </param>
  /// <exception cref="ArgumentException">
  /// <paramref name="capacity"/> is not a power of two, is less than 16,
  /// or exceeds 2^30.
  /// </exception>
  public BoundedMpmcQueue (
    uint capacity,
    ref T start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) {
    Helper.TestCapacity (capacity);

    Debug.Assert (unchecked(tail - head) <= capacity);

    this._start = ref start;
    this._stamp = ref stamp;
    this._head = ref head;
    this._tail = ref tail;

    this._shift = BitOperations.Log2 (capacity / 16);
    this.Capacity = capacity;
  }

  /// <summary>
  /// Resets the queue to an empty state with <c>head == tail == 0</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Writes zero to the caller-supplied <c>head</c> and <c>tail</c> positions,
  /// re-initializes the <c>stamp</c> array for an empty queue, and clears the
  /// backing storage when <typeparamref name="T"/> holds references. Any items
  /// still present become unreachable.
  /// </para>
  /// <para>
  /// Callers that own the backing storage and know it is already in the
  /// desired state can skip this method entirely.
  /// </para>
  /// <para>
  /// Must only be called when no producers or consumers are running.
  /// </para>
  /// </remarks>
  public void Initialize () {
    this._head = this._tail = 0;
    Helper.Initialize (this.Capacity, this._shift, ref this._stamp);

    if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
      MemoryMarshal.CreateSpan (ref this._start, (int)this.Capacity).Clear ();
    }
  }

  /// <summary>
  /// Re-initializes the <c>stamp</c> array to match the current
  /// <c>head</c>/<c>tail</c> positions, without modifying them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Use this to resume a queue whose <c>head</c>/<c>tail</c> have been
  /// advanced or restored from a snapshot, or to attach to storage whose
  /// stamp contents are unknown. The caller must ensure
  /// <c>unchecked(tail - head) &lt;= Capacity</c> before calling.
  /// </para>
  /// <para>
  /// Must only be called when no producers or consumers are running.
  /// </para>
  /// </remarks>
  public void Fresh () => Helper.Fresh
    (this.Capacity, this._shift, ref this._stamp, this._head, this._tail);

  /// <inheritdoc />
  public bool TryEnqueue (T item) => Helper.TryEnqueue (
    item,
    this.Capacity,
    this._shift,
    ref this._start,
    ref this._stamp,
    ref this._tail
  );

  /// <inheritdoc />
  public bool TryDequeue ([MaybeNullWhen (false)] out T item)
    => Helper.TryDequeue (
      out item,
      this.Capacity,
      this._shift,
      ref this._start,
      ref this._stamp,
      ref this._head
    );
}

/// <summary>
/// A bounded multi-producer / single-consumer (MPSC) queue.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// <para>
/// This queue is lock-free. Multiple producers may call
/// <see cref="TryEnqueue"/> concurrently, but only one consumer may call
/// <see cref="TryDequeue"/> at a time.
/// </para>
/// <para>
/// The queue does not own its storage. The caller supplies the backing
/// storage via <c>ref</c> parameters and is responsible for keeping it
/// alive for the lifetime of this instance.
/// </para>
/// <para>
/// The <c>head</c> and <c>tail</c> positions must be separated by at least 60
/// bytes so that they never share a cache line. Placing them 64 bytes apart
/// (or in separate cache-line-aligned fields) is recommended;
/// otherwise the producer and consumer will contend on the same line.
/// </para>
/// <para>
/// The <c>stamp</c> array must be initialized before use by calling
/// <see cref="Initialize"/> or <see cref="Fresh"/>.
/// </para>
/// </remarks>
public readonly ref struct BoundedMpscQueue<T> : IBoundedQueue<T> {
  private readonly ref T _start;
  private readonly ref uint _stamp;
  private readonly ref uint _head;
  private readonly ref uint _tail;
  private readonly int _shift;

  /// <inheritdoc />
  public readonly uint Capacity { get; }

  /// <inheritdoc />
  public uint Count {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => Volatile.Read (ref this._tail) - Volatile.Read (ref this._head);
  }

  /// <inheritdoc />
  public bool IsEmpty {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => this.Count is 0;
  }

  /// <summary>
  /// Initializes a new <see cref="BoundedMpscQueue{T}"/> view over
  /// caller-supplied storage.
  /// </summary>
  /// <param name="capacity">
  /// The queue capacity. Must be a power of two in <c>[16, 2^30]</c>.
  /// </param>
  /// <param name="start">
  /// A reference to the first item slot. Must span at least
  /// <c>capacity</c> elements.
  /// </param>
  /// <param name="stamp">
  /// A reference to the stamp array. Must span at least <c>capacity</c>
  /// elements. The mapping from logical index to physical offset is
  /// interleaved; see <see cref="Initialize"/> for details.
  /// </param>
  /// <param name="head">
  /// A reference to the head position. Must be separated from
  /// <paramref name="tail"/> by at least 60 bytes; see the type remarks.
  /// </param>
  /// <param name="tail">
  /// A reference to the tail position. Must satisfy
  /// <c>unchecked(tail - head) &lt;= capacity</c>.
  /// </param>
  /// <exception cref="ArgumentException">
  /// <paramref name="capacity"/> is not a power of two, is less than 16,
  /// or exceeds 2^30.
  /// </exception>
  public BoundedMpscQueue (
    uint capacity,
    ref T start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) {
    Helper.TestCapacity (capacity);

    Debug.Assert (unchecked(tail - head) <= capacity);

    this._start = ref start;
    this._stamp = ref stamp;
    this._head = ref head;
    this._tail = ref tail;

    this._shift = BitOperations.Log2 (capacity / 16);
    this.Capacity = capacity;
  }

  /// <inheritdoc cref="BoundedMpmcQueue{T}.Initialize" />
  public void Initialize () {
    this._head = this._tail = 0;
    Helper.Initialize (this.Capacity, this._shift, ref this._stamp);

    if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
      MemoryMarshal.CreateSpan (ref this._start, (int)this.Capacity).Clear ();
    }
  }

  /// <inheritdoc cref="BoundedMpmcQueue{T}.Fresh" />
  public void Fresh () => Helper.Fresh
    (this.Capacity, this._shift, ref this._stamp, this._head, this._tail);

  private ref uint Stamp (nuint index)
    => ref Helper.Stamp (ref this._stamp, index, this._shift);

  /// <inheritdoc />
  public bool TryEnqueue (T item) => Helper.TryEnqueue (
    item,
    this.Capacity,
    this._shift,
    ref this._start,
    ref this._stamp,
    ref this._tail
  );

  /// <inheritdoc />
  public bool TryDequeue ([MaybeNullWhen (false)] out T item) {
    uint cap = this.Capacity;
    uint mask = cap - 1;

    uint pos = this._head;

    nuint idx = pos & mask;
    uint stamp = Volatile.Read (ref this.Stamp (idx));

    if (stamp == pos + 1) {
      item = Unsafe.Add (ref this._start, idx);
      if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
        Unsafe.Add (ref this._start, idx) = default!;
      }

      Volatile.Write (ref this.Stamp (idx), pos + cap);
      Volatile.Write (ref this._head, pos + 1);
      return true;
    }

    item = default;
    return false;
  }
}

/// <summary>
/// A bounded single-producer / multi-consumer (SPMC) queue.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// <para>
/// This queue is lock-free. Only one producer may call
/// <see cref="TryEnqueue"/> at a time, but multiple consumers may call
/// <see cref="TryDequeue"/> concurrently.
/// </para>
/// <para>
/// The queue does not own its storage. The caller supplies the backing
/// storage via <c>ref</c> parameters and is responsible for keeping it
/// alive for the lifetime of this instance.
/// </para>
/// <para>
/// The <c>head</c> and <c>tail</c> positions must be separated by at
/// least 60 bytes so that they never share a cache line. Placing them
/// 64 bytes apart (or in separate cache-line-aligned fields) is
/// recommended; otherwise the producer and consumers will contend on
/// the same line.
/// </para>
/// <para>
/// The <c>stamp</c> array must be initialized before use by calling
/// <see cref="Initialize"/> or <see cref="Fresh"/>.
/// </para>
/// </remarks>
public readonly ref struct BoundedSpmcQueue<T> : IBoundedQueue<T> {
  private readonly ref T _start;
  private readonly ref uint _stamp;
  private readonly ref uint _head;
  private readonly ref uint _tail;
  private readonly int _shift;

  /// <inheritdoc />
  public readonly uint Capacity { get; }

  /// <inheritdoc />
  public uint Count {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => Volatile.Read (ref this._tail) - Volatile.Read (ref this._head);
  }

  /// <inheritdoc />
  public bool IsEmpty {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => this.Count is 0;
  }

  /// <summary>
  /// Initializes a new <see cref="BoundedSpmcQueue{T}"/> view over
  /// caller-supplied storage.
  /// </summary>
  /// <param name="capacity">
  /// The queue capacity. Must be a power of two in <c>[16, 2^30]</c>.
  /// </param>
  /// <param name="start">
  /// A reference to the first item slot. Must span at least
  /// <c>capacity</c> elements.
  /// </param>
  /// <param name="stamp">
  /// A reference to the stamp array. Must span at least <c>capacity</c>
  /// elements. The mapping from logical index to physical offset is
  /// interleaved; see <see cref="Initialize"/> for details.
  /// </param>
  /// <param name="head">
  /// A reference to the head position. Must be separated from
  /// <paramref name="tail"/> by at least 60 bytes; see the type remarks.
  /// </param>
  /// <param name="tail">
  /// A reference to the tail position. Must satisfy
  /// <c>unchecked(tail - head) &lt;= capacity</c>.
  /// </param>
  /// <exception cref="ArgumentException">
  /// <paramref name="capacity"/> is not a power of two, is less than 16,
  /// or exceeds 2^30.
  /// </exception>
  public BoundedSpmcQueue (
    uint capacity,
    ref T start,
    ref uint stamp,
    ref uint head,
    ref uint tail
  ) {
    Helper.TestCapacity (capacity);

    Debug.Assert (unchecked(tail - head) <= capacity);

    this._start = ref start;
    this._stamp = ref stamp;
    this._head = ref head;
    this._tail = ref tail;

    this._shift = BitOperations.Log2 (capacity / 16);
    this.Capacity = capacity;
  }

  /// <inheritdoc cref="BoundedMpmcQueue{T}.Initialize" />
  public void Initialize () {
    this._head = this._tail = 0;
    Helper.Initialize (this.Capacity, this._shift, ref this._stamp);

    if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
      MemoryMarshal.CreateSpan (ref this._start, (int)this.Capacity).Clear ();
    }
  }

  /// <inheritdoc cref="BoundedMpmcQueue{T}.Fresh" />
  public void Fresh () => Helper.Fresh
    (this.Capacity, this._shift, ref this._stamp, this._head, this._tail);

  private ref uint Stamp (nuint index)
    => ref Helper.Stamp (ref this._stamp, index, this._shift);

  /// <inheritdoc />
  public bool TryEnqueue (T item) {
    uint cap = this.Capacity;
    uint mask = cap - 1;

    uint pos = this._tail;
    nuint idx = pos & mask;
    uint stamp = Volatile.Read (ref this.Stamp (idx));

    if (stamp != pos) {
      return false;
    }

    Unsafe.Add (ref this._start, idx) = item;
    Volatile.Write (ref this.Stamp (idx), pos + 1);
    Volatile.Write (ref this._tail, pos + 1);

    return true;
  }

  /// <inheritdoc />
  public bool TryDequeue ([MaybeNullWhen (false)] out T item)
    => Helper.TryDequeue (
      out item,
      this.Capacity,
      this._shift,
      ref this._start,
      ref this._stamp,
      ref this._head
    );
}

/// <summary>
/// A bounded single-producer / single-consumer (SPSC) queue.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// <para>
/// This queue is lock-free. Exactly one producer may call
/// <see cref="TryEnqueue"/> and exactly one consumer may call
/// <see cref="TryDequeue"/>; neither is safe to call concurrently with
/// another call of the same kind.
/// </para>
/// <para>
/// The queue does not own its storage. The caller supplies the backing
/// storage via <c>ref</c> parameters and is responsible for keeping it
/// alive for the lifetime of this instance.
/// </para>
/// <para>
/// The <c>head</c> and <c>tail</c> positions must be separated by at
/// least 60 bytes so that they never share a cache line. Placing them
/// 64 bytes apart (or in separate cache-line-aligned fields) is
/// recommended; otherwise the producer and consumer will contend on
/// the same line.
/// </para>
/// </remarks>
public readonly ref struct BoundedSpscQueue<T> : IBoundedQueue<T> {
  private readonly ref T _start;
  private readonly ref uint _head;
  private readonly ref uint _tail;

  /// <inheritdoc />
  public readonly uint Capacity { get; }

  /// <inheritdoc />
  public uint Count {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => Volatile.Read (ref this._tail) - Volatile.Read (ref this._head);
  }

  /// <inheritdoc />
  public bool IsEmpty {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => this.Count is 0;
  }

  /// <summary>
  /// Initializes a new <see cref="BoundedSpscQueue{T}"/> view over
  /// caller-supplied storage.
  /// </summary>
  /// <param name="capacity">
  /// The queue capacity. Must be a power of two in <c>[16, 2^30]</c>.
  /// </param>
  /// <param name="start">
  /// A reference to the first item slot. Must span at least
  /// <c>capacity</c> elements.
  /// </param>
  /// <param name="head">
  /// A reference to the head position. Must be separated from
  /// <paramref name="tail"/> by at least 60 bytes; see the type remarks.
  /// </param>
  /// <param name="tail">
  /// A reference to the tail position. Must satisfy
  /// <c>unchecked(tail - head) &lt;= capacity</c>.
  /// </param>
  /// <exception cref="ArgumentException">
  /// <paramref name="capacity"/> is not a power of two, is less than 16,
  /// or exceeds 2^30.
  /// </exception>
  public BoundedSpscQueue (
    uint capacity,
    ref T start,
    ref uint head,
    ref uint tail
  ) {
    Helper.TestCapacity (capacity);

    Debug.Assert (unchecked(tail - head) <= capacity);

    this._start = ref start;
    this._head = ref head;
    this._tail = ref tail;
    this.Capacity = capacity;
  }

  /// <summary>
  /// Resets the queue to an empty state with <c>head == tail == 0</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Writes zero to the caller-supplied <c>head</c> and <c>tail</c> positions
  /// and clears the backing storage when <typeparamref name="T"/> holds
  /// references. Any items still present become unreachable.
  /// </para>
  /// <para>
  /// Callers that own the backing storage and know it is already in the
  /// desired state can skip this method entirely.
  /// </para>
  /// <para>
  /// Must only be called when the producer and consumer are idle.
  /// </para>
  /// </remarks>
  public void Initialize () {
    this._head = this._tail = 0;

    if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
      MemoryMarshal.CreateSpan (ref this._start, (int)this.Capacity).Clear ();
    }
  }

  /// <inheritdoc />
  public bool TryEnqueue (T item) {
    uint cap = this.Capacity;
    uint mask = cap - 1;

    uint head = Volatile.Read (ref this._head);
    uint tail = this._tail;

    if (tail - head >= cap) {
      return false;
    }

    nuint idx = tail & mask;
    Unsafe.Add (ref this._start, idx) = item;
    Volatile.Write (ref this._tail, tail + 1);

    return true;
  }

  /// <inheritdoc />
  public bool TryDequeue ([MaybeNullWhen (false)] out T item) {
    uint cap = this.Capacity;
    uint mask = cap - 1;

    uint head = this._head;
    uint tail = Volatile.Read (ref this._tail);

    if (head == tail) {
      item = default;
      return false;
    }

    nuint idx = head & mask;
    item = Unsafe.Add (ref this._start, idx);
    if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
      Unsafe.Add (ref this._start, idx) = default!;
    }

    Volatile.Write (ref this._head, head + 1);

    return true;
  }
}

/// <summary>
/// A bounded FIFO queue with no synchronization.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// <para>
/// This queue is not thread-safe. It is intended for use in a single
/// thread, or across threads that coordinate externally.
/// </para>
/// <para>
/// The queue does not own its storage. The caller supplies the backing
/// storage via <c>ref</c> parameters and is responsible for keeping it
/// alive for the lifetime of this instance.
/// </para>
/// </remarks>
public readonly ref struct BoundedQueue<T> : IBoundedQueue<T> {
  private readonly ref T _start;
  private readonly ref uint _head;
  private readonly ref uint _tail;

  /// <inheritdoc />
  public readonly uint Capacity { get; }

  /// <inheritdoc />
  public uint Count {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => this._tail - this._head;
  }

  /// <inheritdoc />
  public bool IsEmpty {
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    get => this.Count is 0;
  }

  /// <summary>
  /// Initializes a new <see cref="BoundedQueue{T}"/> view over
  /// caller-supplied storage.
  /// </summary>
  /// <param name="capacity">
  /// The queue capacity. Must be a power of two in <c>[16, 2^30]</c>.
  /// </param>
  /// <param name="start">
  /// A reference to the first item slot. Must span at least
  /// <c>capacity</c> elements.
  /// </param>
  /// <param name="head">
  /// A reference to the head position. Must satisfy
  /// <c>unchecked(tail - head) &lt;= capacity</c>.
  /// </param>
  /// <param name="tail">
  /// A reference to the tail position. Must satisfy
  /// <c>unchecked(tail - head) &lt;= capacity</c>.
  /// </param>
  /// <exception cref="ArgumentException">
  /// <paramref name="capacity"/> is not a power of two, is less than 16,
  /// or exceeds 2^30.
  /// </exception>
  public BoundedQueue (
    uint capacity,
    ref T start,
    ref uint head,
    ref uint tail
  ) {
    Helper.TestCapacity (capacity);

    Debug.Assert (unchecked(tail - head) <= capacity);

    this._start = ref start;
    this._head = ref head;
    this._tail = ref tail;
    this.Capacity = capacity;
  }

  /// <inheritdoc cref="BoundedSpscQueue{T}.Initialize" />
  public void Initialize () {
    this._head = this._tail = 0;

    if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
      MemoryMarshal.CreateSpan (ref this._start, (int)this.Capacity).Clear ();
    }
  }

  /// <inheritdoc />
  public bool TryEnqueue (T item) {
    uint cap = this.Capacity;
    uint mask = cap - 1;

    uint head = this._head;
    uint tail = this._tail;

    if (tail - head >= cap) {
      return false;
    }

    nuint idx = tail & mask;
    Unsafe.Add (ref this._start, idx) = item;
    this._tail = tail + 1;

    return true;
  }

  /// <inheritdoc />
  public bool TryDequeue ([MaybeNullWhen (false)] out T item) {
    uint cap = this.Capacity;
    uint mask = cap - 1;

    uint head = this._head;
    uint tail = this._tail;

    if (head == tail) {
      item = default;
      return false;
    }

    nuint idx = head & mask;
    item = Unsafe.Add (ref this._start, idx);
    if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
      Unsafe.Add (ref this._start, idx) = default!;
    }

    this._head = head + 1;

    return true;
  }
}

#pragma warning restore CA1711 // 标识符应采用正确的后缀

file static class Helper {
  public static void TestCapacity (uint capacity) {
    if (!BitOperations.IsPow2 (capacity)
      || capacity < 16
      || capacity > (1u << 30)) {
      Throw (capacity);
    }

    [DoesNotReturn, StackTraceHidden]
    static void Throw (uint capacity) => throw new ArgumentException (
      $"""
      Capacity must be a power of 2 in [16, 2^30].
      Actual value was {capacity}.
      """
    );
  }

  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public static ref uint Stamp (ref uint stamp, nuint index, int shift) {
    nuint group = index >> shift;
    nuint stride = (nuint)1 << shift;
    nuint offset = index & (stride - 1);

    return ref Unsafe.Add (ref stamp, group + (offset * 16));
  }

  public static void Initialize (uint capacity, int shift, ref uint stamp) {
    var x0 = Vector512<uint>.Indices << shift;

    for (nuint group = 0; group < capacity; group += 16) {
      x0.StoreUnsafe (ref Unsafe.Add (ref stamp, group));
      x0 += Vector512<uint>.One;
    }
  }

  public static void Fresh (
    uint capacity,
    int shift,
    ref uint stamp,
    uint head,
    uint tail
  ) {
    var headV = Vector512.Create (head);
    var tailRV = Vector512.Create (tail - head);

    var mask = Vector512.Create (~(capacity - 1));
    var bias = headV + ~mask;

    var x0 = Vector512<uint>.Indices << shift;

    for (nuint group = 0; group < capacity; group += 16) {
      var prev = x0;

      var delta = bias - prev;
      delta &= mask & Vector512.LessThan (prev, headV);
      prev += delta;

      prev += Vector512<uint>.One & Vector512.LessThan (prev - headV, tailRV);

      prev.StoreUnsafe (ref Unsafe.Add (ref stamp, group));
      x0 += Vector512<uint>.One;
    }
  }

  public static bool TryEnqueue<T> (
    T item,
    uint capacity,
    int shift,
    ref T start,
    ref uint stamp,
    ref uint tail
  ) {
    uint mask = capacity - 1;

    uint pos = Volatile.Read (ref tail);

    while (true) {
      nuint idx = pos & mask;
      ref uint slot = ref Stamp (ref stamp, idx, shift);
      uint stp = Volatile.Read (ref slot);

      int diff = unchecked((int)(stp - pos));

      if (diff is 0) {
        uint prev = Interlocked.CompareExchange (ref tail, pos + 1, pos);
        if (prev == pos) {
          Unsafe.Add (ref start, idx) = item;
          Volatile.Write (ref slot, pos + 1);
          return true;
        }

        pos = prev;
      }
      else if (diff < 0) {
        return false;
      }
      else {
        pos = Volatile.Read (ref tail);
      }
    }
  }

  public static bool TryDequeue<T> (
    [MaybeNullWhen (false)] out T item,
    uint capacity,
    int shift,
    ref T start,
    ref uint stamp,
    ref uint head
  ) {
    uint mask = capacity - 1;

    uint pos = Volatile.Read (ref head);

    while (true) {
      nuint idx = pos & mask;
      ref uint slot = ref Stamp (ref stamp, idx, shift);
      uint stp = Volatile.Read (ref slot);

      int diff = unchecked((int)(stp - pos));

      if (diff is 1) {
        uint prev = Interlocked.CompareExchange (ref head, pos + 1, pos);
        if (prev == pos) {
          item = Unsafe.Add (ref start, idx);
          if (RuntimeHelpers.IsReferenceOrContainsReferences<T> ()) {
            Unsafe.Add (ref start, idx) = default!;
          }

          Volatile.Write (ref slot, pos + capacity);
          return true;
        }

        pos = prev;
      }
      else if (diff < 1) {
        item = default;
        return false;
      }
      else {
        pos = Volatile.Read (ref head);
      }
    }
  }
}
