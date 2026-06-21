
using System.Runtime.InteropServices;
using System.Threading;

namespace Utils;

public interface IBoundedQueue<T> {
  uint Capacity { get; }
  uint Count { get; }
  bool IsEmpty { get; }

  bool TryEnqueue(T item);
  bool TryDequeue(out T item);
}

[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct Paddedulong {
  [FieldOffset(0)]
  public ulong Value;
}

public sealed class BoundedMpmcQueue<T> : IBoundedQueue<T> {
  private readonly uint _capacity;
  private readonly uint _mask;
  private readonly Slot[] _arr;

  private Paddedulong _head = new();
  private Paddedulong _tail = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  public uint Capacity => this._capacity;

  public uint Count => (uint)(Volatile.Read(ref this._tail.Value)
    - Volatile.Read(ref this._head.Value));

  public bool IsEmpty => (uint)(Volatile.Read(ref this._tail.Value)
    - Volatile.Read(ref this._head.Value)) == 0;

  public BoundedMpmcQueue(uint capacity) {
    capacity = System.Math.Clamp(capacity, 4, 1 << 30);
    this._capacity = 1;
    while (this._capacity < capacity) this._capacity <<= 1;
    this._mask = this._capacity - 1;

    this._arr = new Slot[this._capacity];
    for (int i = 0; i < this._capacity; i++) {
      this._arr[i].Stamp = (uint)i;
    }
  }

  public bool TryEnqueue(T item) {
    ulong pos = Volatile.Read(ref this._tail.Value);

    while (true) {
      int idx = (int)(pos & this._mask);
      ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff == 0) {
        ulong prev = Interlocked.CompareExchange(ref this._tail.Value, pos + 1, pos);
        if (prev == pos) {
          this._arr[idx].Item = item;
          Volatile.Write(ref this._arr[idx].Stamp, pos + 1);
          return true;
        }
        pos = prev;
      }
      else if (diff < 0) return false;
      else pos = Volatile.Read(ref this._tail.Value);
    }
  }

  public bool TryDequeue(out T item) {
    ulong pos = Volatile.Read(ref this._head.Value);

    while (true) {
      int idx = (int)(pos & this._mask);
      ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

      long diff = (long)(stamp - pos);

      if (diff == 1) {
        ulong prev = Interlocked.CompareExchange(ref this._head.Value, pos + 1, pos);
        if (prev == pos) {
          item = this._arr[idx].Item;
          this._arr[idx].Item = default!;
          Volatile.Write(ref this._arr[idx].Stamp, pos + this._capacity);
          return true;
        }
        pos = prev;
      }
      else if (diff < 1) {
        item = default!;
        return false;
      }
      else pos = Volatile.Read(ref this._head.Value);
    }
  }
}

public sealed class BoundedMpscQueue<T> : IBoundedQueue<T> {
  private readonly uint _capacity;
  private readonly uint _mask;
  private readonly Slot[] _arr;

  private Paddedulong _head = new();
  private Paddedulong _tail = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  public uint Capacity => this._capacity;

  public uint Count => (uint)(Volatile.Read(ref this._tail.Value)
    - Volatile.Read(ref this._head.Value));

  public bool IsEmpty => (uint)(Volatile.Read(ref this._tail.Value)
    - Volatile.Read(ref this._head.Value)) == 0;

  public BoundedMpscQueue(uint capacity) {
    capacity = System.Math.Clamp(capacity, 4, 1 << 30);
    this._capacity = 1;
    while (this._capacity < capacity) this._capacity <<= 1;
    this._mask = this._capacity - 1;

    this._arr = new Slot[this._capacity];
    for (int i = 0; i < this._capacity; i++) {
      this._arr[i].Stamp = (uint)i;
    }
  }

  public bool TryEnqueue(T item) {
    ulong pos = Volatile.Read(ref this._tail.Value);

    while (true) {
      int idx = (int)(pos & this._mask);
      ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff == 0) {
        ulong prev = Interlocked.CompareExchange(ref this._tail.Value, pos + 1, pos);
        if (prev == pos) {
          this._arr[idx].Item = item;
          Volatile.Write(ref this._arr[idx].Stamp, pos + 1);
          return true;
        }
        pos = prev;
      }
      else if (diff < 0) return false;
      else pos = Volatile.Read(ref this._tail.Value);
    }
  }

  public bool TryDequeue(out T item) {
    ulong pos = this._head.Value;
    int idx = (int)(pos & this._mask);
    ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

    if (stamp == pos + 1) {
      item = this._arr[idx].Item;
      this._arr[idx].Item = default!;
      Volatile.Write(ref this._arr[idx].Stamp, pos + this._capacity);
      Volatile.Write(ref this._head.Value, pos + 1);
      return true;
    }
    item = default!;
    return false;
  }
}

public sealed class BoundedSpmcQueue<T> : IBoundedQueue<T> {
  private readonly uint _capacity;
  private readonly uint _mask;
  private readonly Slot[] _arr;

  private Paddedulong _head = new();
  private Paddedulong _tail = new();

  private struct Slot {
    public ulong Stamp;
    public T Item;
  }

  public uint Capacity => this._capacity;

  public uint Count => (uint)(Volatile.Read(ref this._tail.Value)
    - Volatile.Read(ref this._head.Value));

  public bool IsEmpty => (uint)(Volatile.Read(ref this._tail.Value)
    - Volatile.Read(ref this._head.Value)) == 0;

  public BoundedSpmcQueue(uint capacity) {
    capacity = System.Math.Clamp(capacity, 4, 1 << 30);
    this._capacity = 1;
    while (this._capacity < capacity) this._capacity <<= 1;
    this._mask = this._capacity - 1;

    this._arr = new Slot[this._capacity];
    for (int i = 0; i < this._capacity; i++) {
      this._arr[i].Stamp = (uint)i;
    }
  }

  public bool TryEnqueue(T item) {
    ulong pos = Volatile.Read(ref this._tail.Value);
    int idx = (int)(pos & this._mask);
    ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

    if (stamp != pos) return false;

    this._arr[idx].Item = item;
    Volatile.Write(ref this._arr[idx].Stamp, pos + 1);
    Volatile.Write(ref this._tail.Value, pos + 1);

    return true;
  }

  public bool TryDequeue(out T item) {
    ulong pos = Volatile.Read(ref this._head.Value);

    while (true) {
      int idx = (int)(pos & this._mask);
      ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff == 1) {
        ulong prev = Interlocked.CompareExchange(ref this._head.Value, pos + 1, pos);
        if (prev == pos) {
          item = this._arr[idx].Item;
          this._arr[idx].Item = default!;

          Volatile.Write(ref this._arr[idx].Stamp, pos + this._capacity);
          return true;
        }
        pos = Volatile.Read(ref this._head.Value);
      }
      else if (diff < 1) {
        item = default!;
        return false;
      }
      else pos = Volatile.Read(ref this._head.Value);
    }
  }
}

public sealed class BoundedSpscQueue<T> : IBoundedQueue<T> {
  private readonly uint _capacity;
  private readonly uint _mask;
  private readonly T[] _arr;

  private Paddedulong _head = new();
  private Paddedulong _tail = new();

  public BoundedSpscQueue(uint capacity) {
    capacity = System.Math.Clamp(capacity, 4, 1 << 30);
    this._capacity = 1;
    while (this._capacity < capacity) this._capacity <<= 1;
    this._mask = this._capacity - 1;

    this._arr = new T[this._capacity];
  }

  public uint Capacity => this._capacity;

  public uint Count => (uint)(Volatile.Read(ref this._tail.Value)
    - Volatile.Read(ref this._head.Value));

  public bool IsEmpty => (uint)(Volatile.Read(ref this._tail.Value)
    - Volatile.Read(ref this._head.Value)) == 0;

  public bool TryEnqueue(T item) {
    ulong head = Volatile.Read(ref this._head.Value);
    ulong tail = this._tail.Value;

    if (tail - head >= this._capacity) return false;

    this._arr[tail & this._mask] = item;
    Volatile.Write(ref this._tail.Value, tail + 1);

    return true;
  }

  public bool TryDequeue(out T item) {
    ulong head = this._head.Value;
    ulong tail = Volatile.Read(ref this._tail.Value);

    if (head == tail) {
      item = default!;
      return false;
    }

    int idx = (int)(head & this._mask);
    item = this._arr[idx];
    this._arr[idx] = default!;
    Volatile.Write(ref this._head.Value, head + 1);

    return true;
  }
}
