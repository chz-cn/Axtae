
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Axtae;

public interface IBoundedQueue<T> {
  uint Capacity { get; }
  uint Count { get; }
  bool IsEmpty { get; }

  bool TryEnqueue(T item);
  bool TryDequeue(out T item);
}

[StructLayout(LayoutKind.Explicit, Size = 72)]
public struct Padded {
  [FieldOffset(0)]
  public ulong Head;
  [FieldOffset(64)]
  public ulong Tail;
}

public sealed class BoundedMpmcQueue<T> : IBoundedQueue<T> {
  private readonly Slot[] _arr;

  private Padded _pos = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  public uint Capacity { get; }

  public uint Count => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head));

  public bool IsEmpty => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head)) is 0;

  public BoundedMpmcQueue(uint capacity) {
    capacity = Math.Clamp(capacity, 4, 1 << 30);
    var cap = 1u;
    while (cap < capacity) cap <<= 1;

    var arr = new Slot[cap];
    for (nuint i = 0; i < (uint)arr.Length; i++)
      arr[i].Stamp = i;

    this.Capacity = cap;
    this._arr = arr;
  }

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

public sealed class BoundedMpscQueue<T> : IBoundedQueue<T> {
  private readonly Slot[] _arr;

  private Padded _pos = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  public uint Capacity { get; }

  public uint Count => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head));

  public bool IsEmpty => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head)) is 0;

  public BoundedMpscQueue(uint capacity) {
    capacity = Math.Clamp(capacity, 4, 1 << 30);
    var cap = 1u;
    while (cap < capacity) cap <<= 1;

    var arr = new Slot[cap];
    for (nuint i = 0; i < (uint)arr.Length; i++)
      arr[i].Stamp = i;

    this.Capacity = cap;
    this._arr = arr;
  }

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

public sealed class BoundedSpmcQueue<T> : IBoundedQueue<T> {
  private readonly Slot[] _arr;

  private Padded _pos = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  public uint Capacity { get; }

  public uint Count => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head));

  public bool IsEmpty => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head)) is 0;

  public BoundedSpmcQueue(uint capacity) {
    capacity = Math.Clamp(capacity, 4, 1 << 30);
    var cap = 1u;
    while (cap < capacity) cap <<= 1;

    var arr = new Slot[cap];
    for (nuint i = 0; i < (uint)arr.Length; i++)
      arr[i].Stamp = i;

    this.Capacity = cap;
    this._arr = arr;
  }

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

public sealed class BoundedSpscQueue<T> : IBoundedQueue<T> {
  private readonly T[] _arr;

  private Padded _pos = new();

  public BoundedSpscQueue(uint capacity) {
    capacity = Math.Clamp(capacity, 4, 1 << 30);
    var cap = 1u;
    while (cap < capacity) cap <<= 1;

    this.Capacity = cap;
    this._arr = (new T[cap]);
  }

  public uint Capacity { get; }

  public uint Count => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head));

  public bool IsEmpty => (uint)(Volatile.Read(ref this._pos.Tail)
    - Volatile.Read(ref this._pos.Head)) is 0;

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
