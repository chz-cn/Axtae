
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Axtae;

/// <summary>
/// Represents a bounded FIFO queue with non‑blocking operations.
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
  bool TryEnqueue(T item);

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
  bool TryDequeue(out T item);
}

/// <summary>
/// Padding structure to avoid false sharing between head and tail positions.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 72)]
internal struct Padded {
  [FieldOffset(0)]
  public ulong Head;

  [FieldOffset(64)]
  public ulong Tail;
}

/// <summary>
/// A bounded multi-producer / multi-consumer (MPMC) queue.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// This queue is thread-safe and lock-free.
/// </remarks>
public sealed class BoundedMpmcQueue<T> : IBoundedQueue<T> {
  private readonly Slot[] _arr;
  private Padded _pos = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  /// <inheritdoc />
  public uint Capacity { get; }

  /// <inheritdoc />
  public uint Count => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head));

  /// <inheritdoc />
  public bool IsEmpty => this.Count is 0;

  /// <summary>
  /// Initializes a new <see cref="BoundedMpmcQueue{T}"/> with the given
  /// capacity.
  /// </summary>
  /// <param name="capacity">
  /// The desired capacity  clamped to [4, 2^30] and rounded up to a power of
  /// two.
  /// </param>
  public BoundedMpmcQueue(uint capacity) {
    capacity = Math.Clamp(capacity, 4, 1 << 30);
    capacity = BitOperations.RoundUpToPowerOf2(capacity);

    var arr = new Slot[capacity];
    for (nuint i = 0; i < (uint)arr.Length; i++)
      arr[i].Stamp = i;

    this.Capacity = capacity;
    this._arr = arr;
  }

  /// <inheritdoc />
  public bool TryEnqueue(T item) {
    var arr = this._arr;
    var mask = this.Capacity - 1;

    ulong pos = Volatile.Read(ref this._pos.Tail);

    while (true) {
      int idx = (int)(pos & mask);
      ulong stamp = Volatile.Read(ref arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff is 0) {
        ulong prev = Interlocked.CompareExchange(
          ref this._pos.Tail, pos + 1, pos);
        if (prev == pos) {
          arr[idx].Item = item;
          Volatile.Write(ref arr[idx].Stamp, pos + 1);
          return true;
        }
        pos = prev;
      }
      else if (diff < 0) return false;
      else pos = Volatile.Read(ref this._pos.Tail);
    }
  }

  /// <inheritdoc />
  public bool TryDequeue(out T item) {
    var arr = this._arr;
    var cap = this.Capacity;
    var mask = cap - 1;

    ulong pos = Volatile.Read(ref this._pos.Head);

    while (true) {
      int idx = (int)(pos & mask);
      ulong stamp = Volatile.Read(ref arr[idx].Stamp);

      long diff = (long)(stamp - pos);

      if (diff is 1) {
        ulong prev = Interlocked.CompareExchange(
          ref this._pos.Head, pos + 1, pos);
        if (prev == pos) {
          item = arr[idx].Item;
          arr[idx].Item = default!;
          Volatile.Write(ref arr[idx].Stamp, pos + cap);
          return true;
        }
        pos = prev;
      }
      else if (diff < 1) {
        item = default!;
        return false;
      }
      else pos = Volatile.Read(ref this._pos.Head);
    }
  }
}

/// <summary>
/// A bounded multi-producer / single-consumer (MPSC) queue.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// This queue is lock-free.
/// </remarks>
public sealed class BoundedMpscQueue<T> : IBoundedQueue<T> {
  private readonly Slot[] _arr;
  private Padded _pos = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  /// <inheritdoc />
  public uint Capacity { get; }

  /// <inheritdoc />
  public uint Count => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head));

  /// <inheritdoc />
  public bool IsEmpty => this.Count is 0;

  /// <summary>
  /// Initializes a new <see cref="BoundedMpscQueue{T}"/> with the given
  /// capacity.
  /// </summary>
  /// <param name="capacity">
  /// The desired capacity  clamped to [4, 2^30] and rounded up to a power of
  /// two.
  /// </param>
  public BoundedMpscQueue(uint capacity) {
    capacity = Math.Clamp(capacity, 4, 1 << 30);
    capacity = BitOperations.RoundUpToPowerOf2(capacity);

    var arr = new Slot[capacity];
    for (nuint i = 0; i < (uint)arr.Length; i++)
      arr[i].Stamp = i;

    this.Capacity = capacity;
    this._arr = arr;
  }

  /// <inheritdoc />
  public bool TryEnqueue(T item) {
    var arr = this._arr;
    var cap = this.Capacity;
    var mask = cap - 1;

    ulong pos = Volatile.Read(ref this._pos.Tail);

    while (true) {
      int idx = (int)(pos & mask);
      ulong stamp = Volatile.Read(ref arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff is 0) {
        ulong prev = Interlocked.CompareExchange(
          ref this._pos.Tail, pos + 1, pos);
        if (prev == pos) {
          arr[idx].Item = item;
          Volatile.Write(ref arr[idx].Stamp, pos + 1);
          return true;
        }
        pos = prev;
      }
      else if (diff < 0) return false;
      else pos = Volatile.Read(ref this._pos.Tail);
    }
  }

  /// <inheritdoc />
  public bool TryDequeue(out T item) {
    var arr = this._arr;
    var cap = this.Capacity;
    var mask = cap - 1;

    ulong pos = this._pos.Head;

    int idx = (int)(pos & mask);
    ulong stamp = Volatile.Read(ref arr[idx].Stamp);

    if (stamp == pos + 1) {
      item = arr[idx].Item;
      arr[idx].Item = default!;
      Volatile.Write(ref arr[idx].Stamp, pos + cap);
      Volatile.Write(ref this._pos.Head, pos + 1);
      return true;
    }
    item = default!;
    return false;
  }
}

/// <summary>
/// A bounded single-producer / multi-consumer (SPMC) queue.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// This queue is lock-free.
/// </remarks>
public sealed class BoundedSpmcQueue<T> : IBoundedQueue<T> {
  private readonly Slot[] _arr;
  private Padded _pos = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  /// <inheritdoc />
  public uint Capacity { get; }

  /// <inheritdoc />
  public uint Count => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head));

  /// <inheritdoc />
  public bool IsEmpty => this.Count is 0;

  /// <summary>
  /// Initializes a new <see cref="BoundedSpmcQueue{T}"/> with the given
  /// capacity.
  /// </summary>
  /// <param name="capacity">
  /// The desired capacity  clamped to [4, 2^30] and rounded up to a power of
  /// two.
  /// </param>
  public BoundedSpmcQueue(uint capacity) {
    capacity = Math.Clamp(capacity, 4, 1 << 30);
    capacity = BitOperations.RoundUpToPowerOf2(capacity);

    var arr = new Slot[capacity];
    for (nuint i = 0; i < (uint)arr.Length; i++)
      arr[i].Stamp = i;

    this.Capacity = capacity;
    this._arr = arr;
  }

  /// <inheritdoc />
  public bool TryEnqueue(T item) {
    var arr = this._arr;
    var cap = this.Capacity;
    var mask = cap - 1;

    ulong pos = Volatile.Read(ref this._pos.Tail);
    int idx = (int)(pos & mask);
    ulong stamp = Volatile.Read(ref arr[idx].Stamp);

    if (stamp != pos) return false;

    arr[idx].Item = item;
    Volatile.Write(ref arr[idx].Stamp, pos + 1);
    Volatile.Write(ref this._pos.Tail, pos + 1);

    return true;
  }

  /// <inheritdoc />
  public bool TryDequeue(out T item) {
    var arr = this._arr;
    var cap = this.Capacity;
    var mask = cap - 1;

    ulong pos = Volatile.Read(ref this._pos.Head);

    while (true) {
      int idx = (int)(pos & mask);
      ulong stamp = Volatile.Read(ref arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff is 1) {
        ulong prev = Interlocked.CompareExchange(
          ref this._pos.Head, pos + 1, pos);
        if (prev == pos) {
          item = arr[idx].Item;
          arr[idx].Item = default!;

          Volatile.Write(ref arr[idx].Stamp, pos + cap);
          return true;
        }
        pos = Volatile.Read(ref this._pos.Head);
      }
      else if (diff < 1) {
        item = default!;
        return false;
      }
      else pos = Volatile.Read(ref this._pos.Head);
    }
  }
}

/// <summary>
/// A bounded single-producer / single-consumer (SPSC) queue.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
public sealed class BoundedSpscQueue<T> : IBoundedQueue<T> {
  private readonly T[] _arr;
  private Padded _pos = new();

  /// <inheritdoc />
  public uint Capacity { get; }

  /// <inheritdoc />
  public uint Count => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head));

  /// <inheritdoc />
  public bool IsEmpty => this.Count is 0;

  /// <summary>
  /// Initializes a new <see cref="BoundedSpscQueue{T}"/> with the given
  /// capacity.
  /// </summary>
  /// <param name="capacity">
  /// The desired capacity  clamped to [4, 2^30] and rounded up to a power of
  /// two.
  /// </param>
  public BoundedSpscQueue(uint capacity) {
    capacity = Math.Clamp(capacity, 4, 1 << 30);
    capacity = BitOperations.RoundUpToPowerOf2(capacity);

    this.Capacity = capacity;
    this._arr = new T[capacity];
  }

  /// <inheritdoc />
  public bool TryEnqueue(T item) {
    var cap = this.Capacity;
    var mask = cap - 1;

    ulong head = Volatile.Read(ref this._pos.Head);
    ulong tail = this._pos.Tail;

    if (tail - head >= cap) return false;

    this._arr[tail & mask] = item;
    Volatile.Write(ref this._pos.Tail, tail + 1);

    return true;
  }

  /// <inheritdoc />
  public bool TryDequeue(out T item) {
    var arr = this._arr;
    var cap = this.Capacity;
    var mask = cap - 1;

    ulong head = this._pos.Head;
    ulong tail = Volatile.Read(ref this._pos.Tail);

    if (head == tail) {
      item = default!;
      return false;
    }

    int idx = (int)(head & mask);
    item = arr[idx];
    arr[idx] = default!;
    Volatile.Write(ref this._pos.Head, head + 1);

    return true;
  }
}
