// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Axtae.Codecs;

using Microsoft.Win32.SafeHandles;

using static System.Text.Encoding;

namespace Axtae;

/// <summary>
/// Provides a high-performance, asynchronous logging system with bounded
/// buffering.
/// </summary>
/// <remarks>
/// <para>
/// Each instance appends formatted log entries to a caller-supplied
/// <see cref="SafeFileHandle"/> via a background task, decoupling producers
/// from I/O.
/// </para>
/// <para>
/// Entries are formatted as
/// <c>[TIMESTAMP] [LEVEL] File(Line) Member\nMessage\n</c>
/// and appended to the file in write order.
/// </para>
/// <para>
/// All public instance methods are thread-safe. Call <see cref="Complete"/>
/// to stop accepting new entries and drain the queue.
/// </para>
/// </remarks>
[StructLayout (LayoutKind.Explicit)]
public sealed partial class Logger {
  // Explicit layout keeps the consumer hot path (_head and _offset)
  // on one cache line and the producer hot path (_tail) on another.

  /// <summary>Next file offset to write at; advanced atomically.</summary>
  [FieldOffset (0)] private long _offset;

  /// <summary>Queue head index, written by consumer.</summary>
  [FieldOffset (8)] private uint _head;

  /// <summary>
  /// Gets the capacity of the internal bounded queue
  /// (number of buffered entries).
  /// </summary>
  [field: FieldOffset (12)] public uint Capacity { get; }

  /// <summary>
  /// The target file handle. Not owned; must outlive the logger.
  /// </summary>
  [FieldOffset (16)] private readonly SafeFileHandle _handle;

  /// <summary>Ring buffer holding queued entries.</summary>
  [FieldOffset (24)] private readonly Entry[] _data;

  /// <summary>Per-slot sequence stamps for the lock-free queue.</summary>
  [FieldOffset (32)] private readonly uint[] _stamp;

  /// <summary>
  /// Scratch buffer for formatting. Used only by the background writer.
  /// </summary>
  [FieldOffset (40)] private readonly byte[] _buffer;

  /// <summary>
  /// UTF-8 encoder for string-to-bytes conversion. Used only by the
  /// background writer; not thread-safe.
  /// </summary>
  [FieldOffset (48)] private readonly System.Text.Encoder _encoder;

  /// <summary>
  /// Gets a <see cref="Task"/> that completes when the background writer has
  /// drained the queue and exited.
  /// </summary>
  /// <remarks>
  /// This task completes after <see cref="Complete"/> has been called and
  /// the queue is empty. It faults only if an unexpected error escapes the
  /// writer loop.
  /// </remarks>
  [field: FieldOffset (56)] public Task Completion { get; }

  /// <summary>
  /// Gets the maximum length (in bytes) of a formatted log entry.
  /// </summary>
  /// <value>
  /// A value of at least 100, clamped during construction.
  /// </value>
  [field: FieldOffset (64)] public ushort MaxEntryLength { get; }

  /// <summary>
  /// Set once by <see cref="Complete"/>; checked by producers and the
  /// background writer to stop accepting and to exit after draining.
  /// </summary>
  [FieldOffset (66)] private bool _completing;

  /// <summary>Queue tail index, written by the producers.</summary>
  [FieldOffset (72)] private uint _tail;

  /// <summary>
  /// Initializes a new <see cref="Logger"/>.
  /// </summary>
  /// <param name="handle">
  /// A writable file handle. The logger appends a header on construction and
  /// appends all subsequent entries to the file. The handle is not owned by
  /// the logger and must remain valid and writable until
  /// <see cref="Completion"/> completes.
  /// </param>
  /// <param name="max_entry_length">
  /// The maximum length in bytes for a single formatted entry.
  /// If less than 100, it is raised to 100. Default is 4096 (4 KiB).
  /// </param>
  /// <param name="capacity">
  /// The capacity of the internal bounded queue. Default is 128 entries.
  /// </param>
  /// <remarks>
  /// <para>
  /// The constructor writes a header with version and environment
  /// information to the file, initializes the queue, and starts a background
  /// task that drains the queue into the file.
  /// </para>
  /// <para>
  /// The background task runs until <see cref="Complete"/> is called and the
  /// queue is empty.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentException">
  /// <paramref name="capacity"/> is not a power of two, is less than 16,
  /// or exceeds 2^30.
  /// </exception>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="handle"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// <paramref name="handle"/> is invalid.
  /// </exception>
  /// <exception cref="ObjectDisposedException">
  /// <paramref name="handle"/> has been closed.
  /// </exception>
  /// <exception cref="NotSupportedException">
  /// In .NET 10 and earlier versions, the file does not support seeking
  /// (pipe or socket).
  /// </exception>
  /// <exception cref="UnauthorizedAccessException">
  /// <paramref name="handle"/> was not opened for writing.
  /// </exception>
  /// <exception cref="IOException">
  /// An I/O error occurred.
  /// </exception>
  public Logger (
    SafeFileHandle handle,
    ushort max_entry_length = 4096,
    ushort capacity = 128
  ) {
    this.MaxEntryLength = Math.Max (max_entry_length, (ushort)100u);

    // Throw if invalid capacity
    _ = new BoundedMpscQueue<Entry> (
      capacity,
      ref Unsafe.NullRef<Entry> (),
      ref Unsafe.NullRef<uint> (),
      ref this._head,
      ref this._tail
    );

    this._handle = handle;
    this._data = new Entry[capacity];
    this._stamp = GC.AllocateUninitializedArray<uint> (capacity);
    this._buffer = GC.AllocateUninitializedArray<byte> (this.MaxEntryLength);
    this._encoder = UTF8.GetEncoder ();
    this.Capacity = capacity;

    var queue = new BoundedMpscQueue<Entry> (
      capacity,
      ref this._data[0],
      ref this._stamp[0],
      ref this._head,
      ref this._tail
    );

    queue.Initialize ();

    Span<byte> sp = stackalloc byte[1024];

    sp[0] = Ascii.LF;
    int len = 1;

    var str = "Version:\n"u8;

    str.CopyTo (sp[len..]);
    len += str.Length;

    str = "  Logger: "u8;
    str.CopyTo (sp[len..]);
    len += str.Length;

    // write Logger version
    // Cover: null branch - never happens in practice
    // (the attribute is always generated by the SDK).
    string? version = typeof (Logger).Assembly
      .GetCustomAttribute<AssemblyInformationalVersionAttribute> ()?
      .InformationalVersion;

    len += UTF8.GetBytes (version, sp[len..]);

    sp[len++] = Ascii.LF;

    // OS version
    str = "  OS    : "u8;
    str.CopyTo (sp[len..]);
    len += str.Length;

    len += UTF8.GetBytes (Environment.OSVersion.VersionString, sp[len..]);

    sp[len++] = Ascii.LF;

    // .NET version
    str = "  .NET  : "u8;
    str.CopyTo (sp[len..]);
    len += str.Length;

    len += UTF8.GetBytes (Environment.Version.ToString (), sp[len..]);

    sp[len++] = Ascii.LF;

    long offset = RandomAccess.GetLength (handle);
    RandomAccess.Write (this._handle, sp[..len], offset);
    this._offset = offset + len;

    this.Completion = Task.Factory.StartNew (
      ProcessLogEntries,
      this,
      default,
      TaskCreationOptions.RunContinuationsAsynchronously,
      TaskScheduler.Default
    );

    static async Task ProcessLogEntries (object? obj) {
      var self = (Logger)obj!;

      while (true) {
        BoundedMpscQueue<Entry> q = new (
          self.Capacity,
          ref self._data[0],
          ref self._stamp[0],
          ref self._head,
          ref self._tail
        );
        if (q.IsEmpty) {
          if (Volatile.Read (ref self._completing)) {
            return;
          }

          await Task.Delay (1).ConfigureAwait (false);
          continue;
        }

        _ = q.TryDequeue (out var entry);

        var span = self._buffer.AsSpan ();
        int len = Parse (ref entry, span, self._encoder);
        long offset = Interlocked.Add (ref self._offset, len) - len;
        RandomAccess.Write (self._handle, span[..len], offset);
      }
    }
  }

  /// <summary>
  /// Enqueues a log entry for asynchronous writing.
  /// </summary>
  /// <param name="entry">The entry to log.</param>
  /// <remarks>
  /// <para>
  /// If the logger is completing, the entry is dropped.
  /// </para>
  /// <para>
  /// The entry is first offered to the bounded queue. If the queue is full,
  /// the entry is formatted and written synchronously on the calling thread,
  /// bypassing the queue. This avoids blocking producers when the buffer is
  /// saturated, at the cost of doing I/O on the caller's thread.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentException">
  /// <see cref="SafeFileHandle"/> is invalid.
  /// </exception>
  /// <exception cref="ObjectDisposedException">
  /// <see cref="SafeFileHandle"/> has been closed.
  /// </exception>
  /// <exception cref="UnauthorizedAccessException">
  /// <see cref="SafeFileHandle"/> was not opened for writing.
  /// </exception>
  /// <exception cref="IOException">
  /// An I/O error occurred.
  /// </exception>
  public void Log (Entry entry) {
    if (Volatile.Read (ref this._completing)) {
      return;
    }

    BoundedMpscQueue<Entry> q = new (
      this.Capacity,
      ref this._data[0],
      ref this._stamp[0],
      ref this._head,
      ref this._tail
    );
    if (q.TryEnqueue (entry)) {
      return;
    }

    var span = this.MaxEntryLength <= 4096
      ? stackalloc byte[this.MaxEntryLength]
      : default;

    byte[]? rented = null;
    if (span.IsEmpty) {
      rented = ArrayPool<byte>.Shared.Rent (this.MaxEntryLength);
      span = rented.AsSpan (0, this.MaxEntryLength);
    }

    try {
      int len = Parse (ref entry, span, UTF8.GetEncoder ());
      long offset = Interlocked.Add (ref this._offset, len) - len;
      RandomAccess.Write (this._handle, span[..len], offset);
    }
    finally {
      if (rented is not null) {
        ArrayPool<byte>.Shared.Return (rented);
      }
    }
  }

  /// <summary>
  /// Signals that no more entries will be logged.
  /// </summary>
  /// <remarks>
  /// After this call, <see cref="Log"/> drops new entries. Entries already
  /// in the queue are still written before <see cref="Completion"/>
  /// completes.
  /// </remarks>
  public void Complete () => Volatile.Write (ref this._completing, true);

  /// <summary>
  /// Formats a log entry into a byte span according to a fixed schema.
  /// </summary>
  /// <param name="entry">The log entry to format.</param>
  /// <param name="span">The output buffer.</param>
  /// <param name="encoder">
  /// The text encoder to use for string conversion.
  /// </param>
  /// <returns>
  /// The number of bytes written to <paramref name="span"/>.
  /// </returns>
  /// <remarks>
  /// The format is:
  /// <c>[TIMESTAMP] [LEVEL] File(Line) Member\nMessage\n</c>
  /// If the output buffer is too small, the line is truncated with an
  /// ellipsis.
  /// </remarks>
  private static int Parse (
    ref Entry entry,
    Span<byte> span,
    System.Text.Encoder encoder
  ) {
    ArgumentNullException.ThrowIfNull (encoder);

    TimeStamp.Format (entry.Timestamp, ref span.GetPinnableReference ());
    int len = TimeStamp.Size;
    span[len++] = Ascii.Space;

    var level = entry.Level switch {
      Level.Debug => "[Debug]"u8,
      Level.Info => "[Info]"u8,
      Level.Warning => "[Warn]"u8,
      Level.Error => "[Error]"u8,
      _ => "[Unknown]"u8
    };

    level.CopyTo (span[len..]);
    len += level.Length;
    span[len++] = Ascii.Space;

    // enocde

    if (!AddString (span, entry.File, ref len, encoder)
      || !AddByte (span, Ascii.OpenParenthesis, ref len)) {
      return len;
    }

    byte l = entry.Line.ToAscii (span[len..]);
    if (l is not 0) {
      len += l;
    }
    else {
      AddDots (span, ref len);
      return len;
    }

    _ = AddBytes (span, ") "u8, ref len)
      && AddString (span, entry.Member, ref len, encoder)
      && AddByte (span, Ascii.LF, ref len)
      && AddString (span, entry.Msg, ref len, encoder)
      && AddByte (span, Ascii.LF, ref len);

    return len;

    static void AddDots (Span<byte> span, ref int used) {
      var End = "...\n"u8;
      while (span.Length - used < End.Length) {
        var status = System.Text.Rune.DecodeLastFromUtf8
          (span[..used], out _, out int count);

        System.Diagnostics.Debug.Assert (status == OperationStatus.Done);
        used -= count;
      }

      End.CopyTo (span[used..]);
      used += End.Length;
    }

    static bool AddByte (Span<byte> span, byte str, ref int used) {
      if (span.Length > used) {
        span[used++] = str;
        return true;
      }

      AddDots (span, ref used);
      return false;
    }

    static bool AddBytes (
      Span<byte> span,
      ReadOnlySpan<byte> str,
      ref int used) {
      int has = span.Length - used;
      int len = str.Length;

      if (has >= len) {
        str.CopyTo (span[used..]);
        used += len;
        return true;
      }

      int write = Math.Min (has, len);
      str[..write].CopyTo (span[used..]);
      used += write;
      AddDots (span, ref used);
      return false;
    }

    static bool AddString (
      Span<byte> span,
      string str,
      ref int used,
      System.Text.Encoder encoder
    ) {
      if (str is { Length: > 0 } msg) {
        encoder.Convert (
          msg,
          span[used..],
          true,
          out _,
          out int bytes_used,
          out bool completed
        );

        used += bytes_used;
        if (!completed) {
          AddDots (span, ref used);
          return false;
        }

        return true;
      }

      return AddByte (span, Ascii.QuestionMark, ref used);
    }
  }
}
