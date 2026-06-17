
using System.Threading;

namespace Utils;

public interface IBoundedQueue<T> {
  uint Capacity { get; }
  uint Count { get; }
  bool IsEmpty { get; }

  bool TryEnqueue(T item);
  bool TryDequeue(out T item);
}

public sealed class BoundedMpmcQueue<T> : IBoundedQueue<T> {
  private readonly uint _capacity;
  private readonly uint _mask;
  private readonly Slot[] _arr;

  private ulong _head = 0;
  private ulong _tail = 0;

  private struct Slot {
    public T Item;
    public ulong Stamp;
  }

  public uint Capacity => this._capacity;
  public uint Count { get => (uint)(Volatile.Read(ref this._tail) - Volatile.Read(ref this._head)); }
  public bool IsEmpty { get => (uint)(Volatile.Read(ref this._tail) - Volatile.Read(ref this._head)) == 0; }

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
    ulong pos = Volatile.Read(ref this._tail);

    while (true) {
      int idx = (int)(pos & this._mask);
      ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff == 0) {
        ulong prev = Interlocked.CompareExchange(ref this._tail, pos + 1, pos);
        if (prev == pos) {
          this._arr[idx].Item = item;
          Volatile.Write(ref this._arr[idx].Stamp, pos + 1);
          return true;
        }
        pos = prev;
      }
      else if (diff < 0) return false;
      else pos = Volatile.Read(ref this._tail);
    }
  }

  public bool TryDequeue(out T item) {
    item = default!;
    ulong pos = Volatile.Read(ref this._head);

    while (true) {
      int idx = (int)(pos & this._mask);
      ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

      long diff = (long)(stamp - pos);

      if (diff == 1) {
        ulong prev = Interlocked.CompareExchange(ref this._head, pos + 1, pos);
        if (prev == pos) {
          item = this._arr[idx].Item;
          this._arr[idx].Item = default!;
          Volatile.Write(ref this._arr[idx].Stamp, pos + this._capacity);
          return true;
        }
        pos = prev;
      }
      else if (diff < 1) return false;
      else pos = Volatile.Read(ref this._head);
    }
  }
}

public sealed class BoundedMpscQueue<T> : IBoundedQueue<T> {
  private readonly uint _capacity;
  private readonly uint _mask;
  private readonly Slot[] _arr;

  private ulong _head = 0;
  private ulong _tail = 0;

  private struct Slot {
    public T Item;
    public ulong Stamp;
  }

  public uint Capacity => this._capacity;
  public uint Count { get => (uint)(Volatile.Read(ref this._tail) - Volatile.Read(ref this._head)); }
  public bool IsEmpty { get => (uint)(Volatile.Read(ref this._tail) - Volatile.Read(ref this._head)) == 0; }

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
    ulong pos = Volatile.Read(ref this._tail);

    while (true) {
      int idx = (int)(pos & this._mask);
      ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff == 0) {
        ulong prev = Interlocked.CompareExchange(ref this._tail, pos + 1, pos);
        if (prev == pos) {
          this._arr[idx].Item = item;
          Volatile.Write(ref this._arr[idx].Stamp, pos + 1);
          return true;
        }
        pos = prev;
      }
      else if (diff < 0) return false;
      else pos = Volatile.Read(ref this._tail);
    }
  }

  public bool TryDequeue(out T item) {
    item = default!;

    ulong pos = this._head;
    int idx = (int)(pos & this._mask);
    ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

    if (stamp == pos + 1) {
      item = this._arr[idx].Item;
      this._arr[idx].Item = default!;
      Volatile.Write(ref this._arr[idx].Stamp, pos + this._capacity);
      Volatile.Write(ref this._head, pos + 1);
      return true;
    }

    return false;
  }
}

public sealed class BoundedSpmcQueue<T> : IBoundedQueue<T> {
  private readonly uint _capacity;
  private readonly uint _mask;
  private readonly Slot[] _arr;

  private ulong _head = 0;
  private ulong _tail = 0;

  private struct Slot {
    public T Item;
    public ulong Stamp;
  }

  public uint Capacity => this._capacity;
  public uint Count { get => (uint)(Volatile.Read(ref this._tail) - Volatile.Read(ref this._head)); }
  public bool IsEmpty { get => (uint)(Volatile.Read(ref this._tail) - Volatile.Read(ref this._head)) == 0; }

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
    ulong pos = Volatile.Read(ref this._tail);
    int idx = (int)(pos & this._mask);
    ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

    if (stamp != pos) return false;

    this._arr[idx].Item = item;
    Volatile.Write(ref this._arr[idx].Stamp, pos + 1);
    Volatile.Write(ref this._tail, pos + 1);

    return true;
  }

  public bool TryDequeue(out T item) {
    item = default!;
    ulong pos = Volatile.Read(ref this._head);

    while (true) {
      int idx = (int)(pos & this._mask);
      ulong stamp = Volatile.Read(ref this._arr[idx].Stamp);

      long diff = unchecked((long)(stamp - pos));

      if (diff == 1) {
        ulong prev = Interlocked.CompareExchange(ref this._head, pos + 1, pos);
        if (prev == pos) {
          item = this._arr[idx].Item;
          this._arr[idx].Item = default!;

          Volatile.Write(ref this._arr[idx].Stamp, pos + this._capacity);
          return true;
        }
        pos = Volatile.Read(ref this._head);
      }
      else if (diff < 1) return false;
      else pos = Volatile.Read(ref this._head);
    }
  }
}

public sealed class BoundedSpscQueue<T> : IBoundedQueue<T> {
  private readonly uint _capacity;
  private readonly uint _mask;
  private readonly T[] _arr;

  private ulong _head = 0;
  private ulong _tail = 0;

  public BoundedSpscQueue(uint capacity) {
    capacity = System.Math.Clamp(capacity, 4, 1 << 30);
    this._capacity = 1;
    while (this._capacity < capacity) this._capacity <<= 1;
    this._mask = this._capacity - 1;

    this._arr = new T[this._capacity];
  }

  public uint Capacity => this._capacity;
  public uint Count { get => (uint)(Volatile.Read(ref this._tail) - Volatile.Read(ref this._head)); }
  public bool IsEmpty { get => (uint)(Volatile.Read(ref this._tail) - Volatile.Read(ref this._head)) == 0; }

  public bool TryEnqueue(T item) {
    ulong head = Volatile.Read(ref this._head);
    ulong tail = this._tail;

    if (tail - head >= this._capacity) return false;

    this._arr[tail & this._mask] = item;
    Volatile.Write(ref this._tail, tail + 1);

    return true;
  }

  public bool TryDequeue(out T item) {
    item = default!;

    ulong head = this._head;
    ulong tail = Volatile.Read(ref this._tail);

    if (head >= tail) return false;

    int idx = (int)(head & this._mask);
    item = this._arr[idx];
    this._arr[idx] = default!;
    Volatile.Write(ref this._head, head + 1);

    return true;
  }
}
