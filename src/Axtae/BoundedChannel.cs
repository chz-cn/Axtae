
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Axtae;

/// <summary>
/// Represents a bounded channel for exchanging messages of type
/// <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of items in the channel.</typeparam>
/// <remarks>
/// <para>
/// A channel consists of a reader and a writer side, each exposed via
/// <see cref="Reader"/> and <see cref="Writer"/> properties. The channel has
/// a fixed capacity and supports asynchronous and synchronous operations.
/// </para>
/// <para>
/// The channel can be in one of three states:
/// <list type="bullet">
/// <item><see cref="Channel.Active"/> - normal operation.</item>
/// <item>
/// <see cref="Channel.Completing"/> - writer has signalled completion,
/// but pending items may still be read.
/// </item>
/// <item>
/// <see cref="Channel.Completed"/> - channel is fully drained and closed;
/// no further reads or writes are allowed.
/// </item>
/// </list>
/// </para>
/// <para>
/// All public members are thread-safe.
/// </para>
/// </remarks>
public interface IChannel<T> {
  /// <summary>
  /// Gets the current state of the channel.
  /// </summary>
  /// <value>
  /// One of <see cref="Channel.Active"/>,
  /// <see cref="Channel.Completing"/>,
  /// <see cref="Channel.Completed"/>.
  /// </value>
  byte State { get; }

  /// <summary>
  /// Gets the maximum number of items the channel can hold.
  /// </summary>
  uint Capacity { get; }

  /// <summary>
  /// Gets the writer endpoint of the channel.
  /// </summary>
  IChannelWriter<T> Writer { get; }

  /// <summary>
  /// Gets the reader endpoint of the channel.
  /// </summary>
  IChannelReader<T> Reader { get; }
}

/// <summary>
/// Provides write operations for a channel.
/// </summary>
/// <typeparam name="T">The type of items to write.</typeparam>
/// <remarks>
/// Writers can attempt synchronous writes, await asynchronous writes,
/// or signal that no more items will be written (<see cref="Complete"/>).
/// </remarks>
public interface IChannelWriter<T> {
  /// <summary>
  /// Attempts to write an item synchronously without blocking.
  /// </summary>
  /// <param name="item">The item to write.</param>
  /// <returns>
  /// <see langword="true"/> if the item was successfully enqueued;
  /// <see langword="false"/> if the channel is full, already completing,
  /// or completed.
  /// </returns>
  bool TryWrite(T item);

  /// <summary>
  /// Asynchronously writes an item to the channel, waiting until space is
  /// available.
  /// </summary>
  /// <param name="item">The item to write.</param>
  /// <returns>
  /// A <see cref="ValueTask"/> that completes when the item has been enqueued.
  /// </returns>
  /// <remarks>
  /// In <see cref="DrainBoundedChannel{T}"/>, pending writes are allowed to
  /// complete when the channel is completing.
  /// In <see cref="RejectBoundedChannel{T}"/>, pending writes are cancelled
  /// immediately when <see cref="Complete"/> is called.
  /// </remarks>
  ValueTask WriteAsync(T item);

  /// <summary>
  /// Signals that no more items will be written to the channel.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This method may be called multiple times;
  /// only the first call has an effect.
  /// After calling <see cref="Complete"/>,
  /// the channel transitions to <see cref="Channel.Completing"/>.
  /// No further writes are allowed via <see cref="TryWrite"/> or
  /// <see cref="WriteAsync"/> - they will fail or be ignored.
  /// </para>
  /// <para>
  /// Readers may still drain pending items. When the queue becomes empty,
  /// the channel moves to <see cref="Channel.Completed"/>.
  /// </para>
  /// </remarks>
  void Complete();
}

/// <summary>
/// Provides a reader side of a channel.
/// </summary>
/// <typeparam name="T">The type of items to read.</typeparam>
/// <remarks>
/// All methods are thread-safe.
/// Readers can attempt synchronous reads, await asynchronous reads, and
/// obtain a task that completes when the channel is fully drained and closed.
/// </remarks>
public interface IChannelReader<T> {
  /// <summary>
  /// Gets a <see cref="Task"/> that completes when the channel has been
  /// fully drained and is in the <see cref="Channel.Completed"/> state.
  /// </summary>
  /// <remarks>
  /// To check the current state of the channel, use
  /// <see cref="IChannel{T}.State"/> instead of relying on
  /// <see cref="Task.IsCompleted"/> of this property.
  /// </remarks>
  Task Completion { get; }

  /// <summary>
  /// Attempts to read an item synchronously without blocking.
  /// </summary>
  /// <param name="item">
  /// When this method returns <see langword="true"/>, contains the read item;
  /// otherwise, the <see langword="default"/> of <typeparamref name="T"/>.
  /// </param>
  /// <returns>
  /// <see langword="true"/> if an item was successfully retrieved;
  /// <see langword="false"/> if the channel is empty or already completed.
  /// </returns>
  bool TryRead(out T item);

  /// <summary>
  /// Asynchronously reads an item from the channel, waiting until one is
  /// available.
  /// </summary>
  /// <returns>
  /// A <see cref="ValueTask{T}"/> that yields the next item.
  /// </returns>
  /// <exception cref="ChannelClosedException">
  /// Thrown if the <see cref="IChannel{T}.State"/> is
  /// <see cref="Channel.Completed"/> and no more items are available.
  /// </exception>
  /// <remarks>
  /// If the channel is empty but not yet completed, the task will wait until
  /// an item is written or the channel is completed.
  /// </remarks>
  ValueTask<T> ReadAsync();
}

/// <summary>
/// Provides factory methods and constants for creating channel instances.
/// </summary>
public static class Channel {
  /// <summary>
  /// The channel is open for both reading and writing.
  /// </summary>
  public const byte Active = 0;

  /// <summary>
  /// The channel is in the process of completing: writing is no longer
  /// allowed,
  /// but remaining items may still be read.
  /// </summary>
  public const byte Completing = 1;

  /// <summary>
  /// The channel is fully completed and empty; all operations are effectively
  /// closed.
  /// </summary>
  public const byte Completed = 2;

  /// <summary>
  /// Creates a new bounded channel with the specified capacity.
  /// </summary>
  /// <typeparam name="T">The type of items in the channel.</typeparam>
  /// <param name="capacity">
  /// The maximum number of items the channel can hold.
  /// </param>
  /// <param name="reject_on_complete">
  /// If <see langword="true"/>, uses a <see cref="RejectBoundedChannel{T}"/>
  /// that cancels pending writes immediately when
  /// <see cref="IChannelWriter{T}.Complete"/> is called.
  /// If <see langword="false"/>, uses a <see cref="DrainBoundedChannel{T}"/>
  /// that allows pending writes to finish before completion.
  /// </param>
  /// <returns>An <see cref="IChannel{T}"/> instance.</returns>
  public static IChannel<T> CreateBounded<T>(
    uint capacity, bool reject_on_complete = false)
    => reject_on_complete
      ? new RejectBoundedChannel<T>(capacity)
      : new DrainBoundedChannel<T>(capacity);
}

/// <summary>
/// Bounded channel that, after completion, allows readers to drain any
/// remaining items.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// <para>
/// In drain mode, calling <see cref="IChannelWriter{T}.Complete"/>
/// transitions the channel to <see cref="Channel.Completing"/>.
/// Writers are not allowed to add new items, but readers can still dequeue
/// pending items. When the queue becomes empty, the channel automatically
/// transitions to <see cref="Channel.Completed"/> and completes the
/// <see cref="IChannelReader{T}.Completion"/> task.
/// </para>
/// <para>
/// All members are thread-safe.
/// </para>
/// </remarks>
public sealed class DrainBoundedChannel<T> : IChannel<T> {
  private readonly BoundedMpmcQueue<T> _queue;
  private readonly SemaphoreSlim _writer_slim;
  private readonly SemaphoreSlim _reader_slim;
  private readonly TaskCompletionSource _completion = new();
  private readonly CancellationTokenSource _cts = new();

  private byte _state = Channel.Active;

  /// <inheritdoc />
  public byte State => Volatile.Read(ref this._state);

  /// <inheritdoc />
  public IChannelWriter<T> Writer { get; }

  /// <inheritdoc />
  public IChannelReader<T> Reader { get; }

  /// <inheritdoc />
  public uint Capacity => this._queue.Capacity;

  /// <summary>
  /// Initializes a new <see cref="DrainBoundedChannel{T}"/> with the given
  /// capacity.
  /// </summary>
  /// <param name="capacity">
  /// The maximum number of items the channel can hold.
  /// </param>
  public DrainBoundedChannel(uint capacity) {
    this._queue = new(capacity);
    int cap = (int)this._queue.Capacity;
    this._writer_slim = new(cap, cap);
    this._reader_slim = new(0, cap);
    this.Writer = new IWriter(this);
    this.Reader = new IReader(this);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

  /// <summary>
  /// Internal implementation of <see cref="IChannelWriter{T}"/> for
  /// <see cref="DrainBoundedChannel{T}"/>.
  /// </summary>
  private sealed class IWriter(DrainBoundedChannel<T> parent)
    : IChannelWriter<T> {
    private readonly DrainBoundedChannel<T> _parent = parent;

    /// <inheritdoc />
    public bool TryWrite(T item) {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is not Channel.Active
        || !p._writer_slim.Wait(0))
        return false;

      _ = p._queue.TryEnqueue(item);
      _ = p._reader_slim.Release();
      return true;
    }

    /// <inheritdoc />
    public async ValueTask WriteAsync(T item) {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is not Channel.Active)
        return;

      await p._writer_slim.WaitAsync().ConfigureAwait(false);

      _ = p._queue.TryEnqueue(item);
      _ = p._reader_slim.Release();
    }

    /// <inheritdoc />
    public void Complete() {
      var p = this._parent;
      byte prev = Interlocked.CompareExchange(
        ref p._state, Channel.Completing, Channel.Active);
      if (prev is not Channel.Active)
        return;

      p.CheckComplet();
    }
  }

  /// <summary>
  /// Internal implementation of <see cref="IChannelReader{T}"/> for
  /// <see cref="DrainBoundedChannel{T}"/>.
  /// </summary>
  private sealed class IReader(DrainBoundedChannel<T> parent)
    : IChannelReader<T> {
    private readonly DrainBoundedChannel<T> _parent = parent;

    /// <inheritdoc />
    public Task Completion => this._parent._completion.Task;

    /// <inheritdoc />
    public bool TryRead(out T item) {
      item = default!;
      var p = this._parent;
      if (Volatile.Read(ref p._state) is Channel.Completed
        || !p._reader_slim.Wait(0))
        return false;

      _ = p._queue.TryDequeue(out item);
      _ = p._writer_slim.Release();
      p.CheckComplet();
      return true;
    }

    /// <inheritdoc />
    public async ValueTask<T> ReadAsync() {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is Channel.Completed)
        throw new ChannelClosedException();

      try {
        await p._reader_slim.WaitAsync(p._cts.Token)
          .ConfigureAwait(false);
      }
      catch (OperationCanceledException) {
        throw new ChannelClosedException();
      }

      _ = p._queue.TryDequeue(out T item);
      _ = p._writer_slim.Release();
      p.CheckComplet();
      return item;
    }
  }
}

/// <summary>
/// A bounded channel that, upon completion, immediately cancels all pending write
/// operations and rejects any further writes. Remaining items are still drained
/// by readers before the channel is fully completed.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <remarks>
/// <para>
/// When <see cref="IChannelWriter{T}.Complete"/> is called,
/// the state transitions to <see cref="Channel.Completing"/> and all writers
/// waiting on <see cref="IChannelWriter{T}.WriteAsync"/> are cancelled.
/// Attempts to write after completion will fail.
/// </para>
/// <para>
/// Readers continue reading until the queue is empty,
/// after which the state  becomes <see cref="Channel.Completed"/> and
/// <see cref="IChannelReader{T}.Completion"/> completes.
/// </para>
/// <para>
/// All members are thread-safe.
/// </para>
/// </remarks>
public sealed class RejectBoundedChannel<T> : IChannel<T> {
  private readonly BoundedMpmcQueue<T> _queue;
  private readonly SemaphoreSlim _writer_slim;
  private readonly SemaphoreSlim _reader_slim;
  private readonly TaskCompletionSource _completion = new();
  private readonly CancellationTokenSource _writer_cts = new();
  private readonly CancellationTokenSource _reader_cts = new();

  private byte _state = Channel.Active;

  /// <inheritdoc />
  public byte State => Volatile.Read(ref this._state);

  /// <inheritdoc />
  public IChannelWriter<T> Writer { get; }

  /// <inheritdoc />
  public IChannelReader<T> Reader { get; }

  /// <inheritdoc />
  public uint Capacity => this._queue.Capacity;

  /// <summary>
  /// Initializes a new <see cref="RejectBoundedChannel{T}"/> with the given
  /// capacity.
  /// </summary>
  /// <param name="capacity">
  /// The maximum number of items the channel can hold.
  /// </param>
  public RejectBoundedChannel(uint capacity) {
    this._queue = new(capacity);
    int cap = (int)this._queue.Capacity;
    this._writer_slim = new(cap, cap);
    this._reader_slim = new(0, cap);
    this.Writer = new IWriter(this);
    this.Reader = new IReader(this);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CheckComplet() {
    if (this._writer_slim.CurrentCount < this._queue.Capacity
      || !this._queue.IsEmpty)
      return;

    byte prev = Interlocked.CompareExchange(
      ref this._state, Channel.Completed, Channel.Completing);
    if (prev is not Channel.Completing)
      return;

    this._reader_cts.Cancel();
    _ = this._completion.TrySetResult();
    this._writer_cts.Dispose();
    this._reader_cts.Dispose();
  }

  /// <summary>
  /// Internal implementation of <see cref="IChannelWriter{T}"/> for
  /// <see cref="RejectBoundedChannel{T}"/>.
  /// </summary>
  private sealed class IWriter(RejectBoundedChannel<T> parent)
    : IChannelWriter<T> {
    private readonly RejectBoundedChannel<T> _parent = parent;

    /// <inheritdoc />
    public bool TryWrite(T item) {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is not Channel.Active
        || !p._writer_slim.Wait(0))
        return false;

      // Double-check state after acquiring the semaphore
      if (Volatile.Read(ref p._state) is not Channel.Active) {
        _ = p._writer_slim.Release();
        return false;
      }

      _ = p._queue.TryEnqueue(item);
      _ = p._reader_slim.Release();
      return true;
    }

    /// <inheritdoc />
    public async ValueTask WriteAsync(T item) {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is not Channel.Active)
        return;

      try {
        await p._writer_slim.WaitAsync(p._writer_cts.Token)
          .ConfigureAwait(false);
      }
      catch (OperationCanceledException) {
        // Completion was called; write is cancelled.
        return;
      }

      // Double-check state after acquiring the semaphore
      if (Volatile.Read(ref p._state) is not Channel.Active) {
        _ = p._writer_slim.Release();
        return;
      }

      _ = p._queue.TryEnqueue(item);
      _ = p._reader_slim.Release();
    }

    /// <inheritdoc />
    public void Complete() {
      var p = this._parent;
      byte prev = Interlocked.CompareExchange(
        ref p._state, Channel.Completing, Channel.Active);
      if (prev is not Channel.Active)
        return;

      p._writer_cts.Cancel();
      p.CheckComplet();
    }
  }

  /// <summary>
  /// Internal implementation of <see cref="IChannelReader{T}"/> for
  /// <see cref="RejectBoundedChannel{T}"/>.
  /// </summary>
  private sealed class IReader(RejectBoundedChannel<T> parent)
    : IChannelReader<T> {
    private readonly RejectBoundedChannel<T> _parent = parent;

    /// <inheritdoc />
    public Task Completion => this._parent._completion.Task;

    /// <inheritdoc />
    public bool TryRead(out T item) {
      item = default!;
      var p = this._parent;
      if (Volatile.Read(ref p._state) is Channel.Completed
        || !p._reader_slim.Wait(0))
        return false;

      _ = p._queue.TryDequeue(out item);
      _ = p._writer_slim.Release();
      p.CheckComplet();
      return true;
    }

    /// <inheritdoc />
    public async ValueTask<T> ReadAsync() {
      var p = this._parent;
      if (Volatile.Read(ref p._state) is Channel.Completed)
        throw new ChannelClosedException();

      try {
        await p._reader_slim.WaitAsync(p._reader_cts.Token)
          .ConfigureAwait(false);
      }
      catch (OperationCanceledException) {
        throw new ChannelClosedException();
      }

      _ = p._queue.TryDequeue(out T item);
      _ = p._writer_slim.Release();
      p.CheckComplet();
      return item;
    }
  }
}
