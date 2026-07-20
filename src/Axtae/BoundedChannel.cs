
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Axtae;

public interface IChannel<T> {
  public byte State { get; }
  IChannelWriter<T> Writer { get; }
  IChannelReader<T> Reader { get; }
}

public interface IChannelWriter<T> {
  bool TryWrite(T item);
  ValueTask WriteAsync(T item);

  void Complete();
}

public interface IChannelReader<T> {
  Task Completion { get; }

  bool TryRead(out T item);
  ValueTask<T> ReadAsync();
}

public interface IBoundedChannel<T> : IChannel<T> {
  uint Capacity { get; }
}

public static class Channel {
  public const byte Active = 0;
  public const byte Completing = 1;
  public const byte Completed = 2;

  public static IBoundedChannel<T> CreateBounded<T>(
    uint capacity, bool reject_on_complete = false)
    => reject_on_complete
    ? new RejectBoundedChannel<T>(capacity)
    : new DrainBoundedChannel<T>(capacity);
}

public sealed class DrainBoundedChannel<T> : IBoundedChannel<T> {
  private readonly BoundedMpmcQueue<T> _queue;
  private readonly SemaphoreSlim _writer_slim;
  private readonly SemaphoreSlim _reader_slim;
  private readonly TaskCompletionSource _completion = new();
  private readonly CancellationTokenSource _cts = new();

  private byte _state = Channel.Active;

  public byte State => Volatile.Read(ref this._state);
  public IChannelWriter<T> Writer { get; }
  public IChannelReader<T> Reader { get; }

  public uint Capacity => this._queue.Capacity;

  public DrainBoundedChannel(uint capacity) {
    this._queue = new(capacity);
    int cap = (int)this._queue.Capacity;
    this._writer_slim = new(cap, cap);
    this._reader_slim = new(0, cap);
    this.Writer = new IWriter(this);
    this.Reader = new IReader(this);
  }

  private void CheckComplet() {
    if (this._writer_slim.CurrentCount < this._queue.Capacity
      || !this._queue.IsEmpty) return;

    byte prev = Interlocked.CompareExchange(
      ref this._state, Channel.Completed, Channel.Completing);
    if (prev is not Channel.Completing) return;

    this._cts.Cancel();
    _ = this._completion.TrySetResult();
    this._cts.Dispose();
  }

  private sealed class IWriter(DrainBoundedChannel<T> parent)
    : IChannelWriter<T> {
    private readonly DrainBoundedChannel<T> _parent = parent;

    public bool TryWrite(T item) {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is not Channel.Active
        || !p._writer_slim.Wait(0)) return false;

      _ = p._queue.TryEnqueue(item);
      _ = p._reader_slim.Release();
      return true;
    }

    public async ValueTask WriteAsync(T item) {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is not Channel.Active) return;

      await p._writer_slim.WaitAsync().ConfigureAwait(false);

      _ = p._queue.TryEnqueue(item);
      _ = p._reader_slim.Release();
    }

    public void Complete() {
      var p = this._parent;
      byte prev = Interlocked.CompareExchange(
        ref p._state, Channel.Completing, Channel.Active);
      if (prev is not Channel.Active) return;

      p.CheckComplet();
    }
  }

  private sealed class IReader(DrainBoundedChannel<T> parent)
    : IChannelReader<T> {
    private readonly DrainBoundedChannel<T> _parent = parent;

    public Task Completion => this._parent._completion.Task;

    public bool TryRead(out T item) {
      item = default!;
      var p = this._parent;
      if (Volatile.Read(ref p._state) is Channel.Completed
        || !p._reader_slim.Wait(0)) return false;

      _ = p._queue.TryDequeue(out item);
      _ = p._writer_slim.Release();
      p.CheckComplet();
      return true;
    }

    public async ValueTask<T> ReadAsync() {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is Channel.Completed) return default!;

      try {
        await p._reader_slim.WaitAsync(p._cts.Token)
          .ConfigureAwait(false);
      }
      catch (OperationCanceledException) { return default!; }

      _ = p._queue.TryDequeue(out T item);
      _ = p._writer_slim.Release();
      p.CheckComplet();
      return item;
    }
  }
}

public sealed class RejectBoundedChannel<T> : IBoundedChannel<T> {
  private readonly BoundedMpmcQueue<T> _queue;
  private readonly SemaphoreSlim _writer_slim;
  private readonly SemaphoreSlim _reader_slim;
  private readonly TaskCompletionSource _completion = new();
  private readonly CancellationTokenSource _writer_cts = new();
  private readonly CancellationTokenSource _reader_cts = new();

  private byte _state = Channel.Active;

  public byte State => Volatile.Read(ref this._state);
  public IChannelWriter<T> Writer { get; }
  public IChannelReader<T> Reader { get; }

  public uint Capacity => this._queue.Capacity;

  public RejectBoundedChannel(uint capacity) {
    this._queue = new(capacity);
    int cap = (int)this._queue.Capacity;
    this._writer_slim = new(cap, cap);
    this._reader_slim = new(0, cap);
    this.Writer = new IWriter(this);
    this.Reader = new IReader(this);
  }

  private void CheckComplet() {
    if (this._writer_slim.CurrentCount < this._queue.Capacity
      || !this._queue.IsEmpty) return;

    byte prev = Interlocked.CompareExchange(
      ref this._state, Channel.Completed, Channel.Completing);
    if (prev is not Channel.Completing) return;

    this._reader_cts.Cancel();
    _ = this._completion.TrySetResult();
    this._writer_cts.Dispose();
    this._reader_cts.Dispose();
  }

  private sealed class IWriter(RejectBoundedChannel<T> parent)
    : IChannelWriter<T> {
    private readonly RejectBoundedChannel<T> _parent = parent;

    public bool TryWrite(T item) {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is not Channel.Active
        || !p._writer_slim.Wait(0)) return false;

      if (Volatile.Read(ref p._state) is not Channel.Active) {
        _ = p._writer_slim.Release();
        return false;
      }

      _ = p._queue.TryEnqueue(item);
      _ = p._reader_slim.Release();
      return true;
    }

    public async ValueTask WriteAsync(T item) {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is not Channel.Active) return;

      try {
        await p._writer_slim.WaitAsync(p._writer_cts.Token)
          .ConfigureAwait(false);
      }
      catch (OperationCanceledException) { return; }

      if (Volatile.Read(ref p._state) is not Channel.Active) {
        _ = p._writer_slim.Release();
        return;
      }

      _ = p._queue.TryEnqueue(item);
      _ = p._reader_slim.Release();
    }

    public void Complete() {
      var p = this._parent;
      byte prev = Interlocked.CompareExchange(
        ref p._state, Channel.Completing, Channel.Active);
      if (prev is not Channel.Active) return;

      p._writer_cts.Cancel();
      p.CheckComplet();
    }
  }

  private sealed class IReader(RejectBoundedChannel<T> parent)
    : IChannelReader<T> {
    private readonly RejectBoundedChannel<T> _parent = parent;

    public Task Completion => this._parent._completion.Task;

    public bool TryRead(out T item) {
      item = default!;
      var p = this._parent;
      if (Volatile.Read(ref p._state) is Channel.Completed
        || !p._reader_slim.Wait(0)) return false;

      switch (Volatile.Read(ref p._state)) {
        case Channel.Active:
          _ = p._queue.TryDequeue(out item);
          _ = p._writer_slim.Release();
          return true;

        case Channel.Completing:
          _ = p._queue.TryDequeue(out item);
          _ = p._writer_slim.Release();
          p.CheckComplet();
          return true;

        default:
          _ = p._reader_slim.Release();
          return false;
      }
    }

    public async ValueTask<T> ReadAsync() {
      T item = default!;
      var p = this._parent;
      if (Volatile.Read(ref p._state) is Channel.Completed) return item;

      try {
        await p._reader_slim.WaitAsync(p._reader_cts.Token)
          .ConfigureAwait(false);
      }
      catch (OperationCanceledException) { return item; }

      switch (Volatile.Read(ref p._state)) {
        case Channel.Active:
          _ = p._queue.TryDequeue(out item);
          _ = p._writer_slim.Release();
          return item;

        case Channel.Completing:
          _ = p._queue.TryDequeue(out item);
          _ = p._writer_slim.Release();
          p.CheckComplet();
          return item;

        default:
          _ = p._reader_slim.Release();
          return item;
      }
    }
  }
}
