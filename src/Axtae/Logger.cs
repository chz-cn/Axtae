
using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Axtae.Codecs;

using static System.Text.Encoding;

namespace Axtae;

/// <summary>
/// Provides a high-performance, asynchronous logging system with bounded
/// buffering.
/// </summary>
/// <remarks>
/// <para>
/// Each instance manages its own log file and internal channel. Log entries
/// are written asynchronously by a background task, decoupling producers from
/// I/O.
/// </para>
/// <para>
/// The logger supports four severity levels (<see cref="Level"/>), automatic
/// caller information (file, member, line), and timestamping. Entries are
/// formatted in a fixed human-readable schema.
/// </para>
/// <para>
/// All public instance methods are thread-safe. Call <see cref="Complete"/>
/// to close the logger and drain remaining entries.
/// </para>
/// </remarks>
public sealed class Logger {
  /// <summary>
  /// Defines the severity levels for log entries.
  /// </summary>
  public enum Level {
    /// <summary>Debugging information, used only in DEBUG builds.</summary>
    Debug = 1,
    /// <summary>Informational messages.</summary>
    Info = 2,
    /// <summary>Warning conditions that are not errors.</summary>
    Warning = 3,
    /// <summary>Error conditions that require attention.</summary>
    Error = 4
  }

  /// <summary>
  /// The maximum length (in bytes) of a formatted log entry.
  /// </summary>
  /// <value>
  /// A value between 1 and 100, clamped during construction.
  /// </value>
  public readonly ushort MaxEntryLength;

  /// <summary>
  /// The full path to the log file used by this instance.
  /// </summary>
  public readonly string LogFilePath;

  /// <summary>
  /// The current capacity of the internal channel (number of buffered entries).
  /// </summary>
  public uint Size => this._channel.Capacity;

  // Internal channel for buffering log entries.
  private readonly IChannel<LogEntry> _channel;

  // File writer instance responsible for I/O.
  private readonly FileWriter _writer;

  /// <summary>
  /// Initializes a new <see cref="Logger"/>.
  /// </summary>
  /// <param name="log_file_path">
  /// The full path to the log file.
  /// The directory is created if it does not exist.
  /// </param>
  /// <param name="max_entry_length">
  /// The maximum length in bytes for a single formatted entry.
  /// If less than 100, it is raised to 100. Default is 4096 (4 KiB).
  /// </param>
  /// <param name="size">
  /// The capacity of the internal bounded channel.
  /// If less than 4, it is raised to 4. Default is 128 entries.
  /// </param>
  /// <exception cref="ArgumentException">
  /// Thrown if <paramref name="log_file_path"/> is <see langword="null"/>,
  /// empty, or whitespace.
  /// </exception>
  /// <remarks>
  /// <para>
  /// The constructor creates the log file (appending a header with version
  /// and environment information) and starts a background task that reads
  /// entries from the channel and writes them to the file.
  /// </para>
  /// <para>
  /// The background task continues until the channel is completed
  /// (via <see cref="Complete"/>), then disposes the file writer.
  /// </para>
  /// </remarks>
  public Logger(string log_file_path,
    ushort max_entry_length = 4096, ushort size = 128) {
    ArgumentException.ThrowIfNullOrWhiteSpace(log_file_path);

    this.MaxEntryLength = Math.Max(max_entry_length, (ushort)100u);
    this.LogFilePath = log_file_path;
    this._channel = Channel.CreateBounded<LogEntry>(size);
    this._writer = new FileWriter(log_file_path);

    _ = Task.Run(async () => {
      using var writer = this._writer;
      byte[] buffer = new byte[this.MaxEntryLength];
      var encoder = UTF8.GetEncoder();
      var reader = this._channel.Reader;

      while (true) {
        var entry = await reader.ReadAsync()
          .ConfigureAwait(false);

        var span = buffer.AsSpan();
        Write(span, ref entry, encoder);

        // Drain any additional entries that may have arrived while
        // we were formatting the first one.
        while (reader.TryRead(out var item))
          Write(span, ref item, encoder);
      }

      void Write(Span<byte> span, ref LogEntry entry,
        System.Text.Encoder encoder) {
        uint len = Parse(ref entry, span, encoder);

        writer.Write(span[..(int)len]);

        if (entry.Level is Level.Error) writer.Flush();
      }
    });
  }

  /// <summary>
  /// Represents a single log entry with all contextual information.
  /// </summary>
  /// <remarks>
  /// This struct is immutable after construction, except for the
  /// <see cref="Timestamp"/> field which is mutated by the logger before
  /// enqueuing. It is passed by reference internally to avoid copying.
  /// </remarks>
  internal struct LogEntry {
    /// <summary>The log message.</summary>
    public string Msg { readonly get; init; }

    /// <summary>The source file path from which the log was called.</summary>
    public string File { readonly get; init; }

    /// <summary>
    /// The member name (method/property) from which the log was called.
    /// </summary>
    public string Member { readonly get; init; }

    /// <summary>The timestamp when the log entry was created.</summary>
    /// <remarks>
    /// The buffer is filled by <see cref="TimeStamp.GetStamp"/>.
    /// It is the only mutable field; set by the logger before enqueuing.
    /// </remarks>
    public TimeStamp.Buffer Timestamp;

    /// <summary>The source line number.</summary>
    public int Line { readonly get; init; }

    /// <summary>The severity level of the log entry.</summary>
    public Level Level { readonly get; init; }
  }

  /// <summary>
  /// Internal method that enqueues a log entry into the channel.
  /// </summary>
  /// <param name="level">Severity level.</param>
  /// <param name="msg">The message.</param>
  /// <param name="file">Source file path.</param>
  /// <param name="member">Member name.</param>
  /// <param name="line">Line number.</param>
  /// <remarks>
  /// If the message is <see langword="null"/> or whitespace,
  /// the call is ignored.
  /// If the channel is not <see cref="Channel.Active"/>,
  /// the entry is dropped.
  /// The method attempts a synchronous write; if the channel is full,
  /// it falls back to an asynchronous write (fire-and-forget).
  /// </remarks>
  public void Log(Level level, string msg,
    string file, string member, int line) {
    if (string.IsNullOrWhiteSpace(msg)
      || this._channel.State is not Channel.Active) return;

    var entry = new LogEntry {
      Msg = msg,
      Level = level,
      File = file,
      Member = member,
      Line = line
    };

    TimeStamp.GetStamp(entry.Timestamp);

    var writer = this._channel.Writer;
    if (writer.TryWrite(entry)) return;

    _ = writer.WriteAsync(entry).AsTask();
  }

  /// <summary>
  /// Closes the logger, preventing further log entries from being enqueued.
  /// </summary>
  /// <remarks>
  /// After calling this method, all pending entries will still be written,
  /// but new log calls will be ignored. The background task will complete
  /// once the channel is drained.
  /// </remarks>
  public void Complete() => this._channel.Writer.Complete();

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
  /// <c>[TIMESTAMP] [LEVEL] File(Line) --> Member\nMessage\n</c>
  /// If the output buffer is too small, the line is truncated with an
  /// ellipsis.
  /// </remarks>
  private static uint Parse(ref LogEntry entry, Span<byte> span,
    System.Text.Encoder encoder) {
    ArgumentNullException.ThrowIfNull(encoder);

    ((ReadOnlySpan<byte>)entry.Timestamp).CopyTo(span);
    int len = TimeStamp.Size;
    span[len++] = Ascii.Space;

    ReadOnlySpan<byte> level = entry.Level switch {
      Level.Debug => "[Debug]"u8,
      Level.Info => "[Info]"u8,
      Level.Warning => "[Warn]"u8,
      Level.Error => "[Error]"u8,
      _ => "[Unknown]"u8
    };

    level.CopyTo(span[len..]);
    len += level.Length;
    span[len++] = Ascii.Space;

    // enocde

    if (!AddString(span, entry.File, ref len, encoder)
      || !AddByte(span, Ascii.OpenParenthesis, ref len))
      return (uint)len;

    byte l = entry.Line.ToAscii(span[len..]);
    if (l is not 0) len += l;
    else {
      AddDots(span, ref len);
      return (uint)len;
    }

    if (AddBytes(span, ") --> "u8, ref len)
      && AddString(span, entry.Member, ref len, encoder)
      && AddByte(span, Ascii.LF, ref len)
      && AddString(span, entry.Msg, ref len, encoder)
      && AddByte(span, Ascii.LF, ref len))
      return (uint)len;

    return (uint)len;

    static void AddDots(Span<byte> span, ref int used) {
      ReadOnlySpan<byte> End = "...\n"u8;
      while (span.Length - used < End.Length) {
        var status = System.Text.Rune.DecodeLastFromUtf8(
          span[..used], out _, out int count);

        System.Diagnostics.Debug.Assert(status == OperationStatus.Done);
        used -= count;
      }

      End.CopyTo(span[used..]);
      used += End.Length;
    }

    static bool AddByte(Span<byte> span, byte str, ref int used) {
      if (span.Length > used) {
        span[used++] = str;
        return true;
      }

      AddDots(span, ref used);
      return false;
    }

    static bool AddBytes(
      Span<byte> span,
      ReadOnlySpan<byte> str,
      ref int used) {
      int has = span.Length - used;
      int len = str.Length;

      if (has >= len) {
        str.CopyTo(span[used..]);
        used += len;
        return true;
      }

      int write = Math.Min(has, len);
      str[..write].CopyTo(span[used..]);
      used += write;
      AddDots(span, ref used);
      return false;
    }

    static bool AddString(Span<byte> span, string str, ref int used,
      System.Text.Encoder encoder) {
      if (str is { Length: > 0 } msg) {
        encoder.Convert(
          msg,
          span[used..],
          true,
          out _,
          out int bytes_used,
          out bool completed
        );

        used += bytes_used;
        if (!completed) {
          AddDots(span, ref used);
          return false;
        }
        else return true;
      }

      return AddByte(span, Ascii.QuestionMark, ref used);
    }
  }

  /// <summary>
  /// Wraps a <see cref="FileStream"/> to write log entries to the log file
  /// and also writes a header with version and environment information on
  /// creation.
  /// </summary>
  /// <remarks>
  /// This class is instantiated once per logger instance and is disposed
  /// by the background task when the logger is completed.
  /// </remarks>
  private sealed class FileWriter : IDisposable {
    private readonly FileStream _stream;

    /// <summary>
    /// Initializes the file writer, creates the directory, opens the file in
    /// append mode, and writes a header containing version and environment
    /// information.
    /// </summary>
    /// <param name="file_path">The full path to the log file.</param>
    public FileWriter(string file_path) {
      string? dir = Path.GetDirectoryName(file_path);

      System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(file_path));
      _ = Directory.CreateDirectory(dir!);

      var stream = new FileStream(file_path,
        FileMode.Append,
        FileAccess.Write,
        FileShare.Read,
        8 * 1024,
        FileOptions.None);

      stream.WriteByte(Ascii.LF);

      // write Logger version
      // Cover: null branch - never happens in practice
      // (the attribute is always generated by the SDK).
      var version = typeof(Logger).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion;

      stream.Write("Version:\n"u8);
      stream.Write("  Logger: "u8);
      // Cover: See local init
      stream.Write(version is null ? "Unknown"u8 : UTF8.GetBytes(version));
      stream.WriteByte(Ascii.LF);

      // OS version
      stream.Write("  OS    : "u8);
      stream.Write(UTF8.GetBytes(Environment.OSVersion.VersionString));
      stream.WriteByte(Ascii.LF);

      // .NET version
      stream.Write("  .NET  : "u8);
      stream.Write(UTF8.GetBytes(Environment.Version.ToString()));
      stream.WriteByte(Ascii.LF);

      this._stream = stream;
    }

    /// <summary>Writes a byte span to the underlying file stream.</summary>
    public void Write(ReadOnlySpan<byte> what) => this._stream.Write(what);

    /// <summary>Flushes the underlying file stream.</summary>
    public void Flush() => this._stream.Flush();

    /// <summary>Disposes the underlying file stream.</summary>
    public void Dispose() => this._stream.Dispose();
  }
}
