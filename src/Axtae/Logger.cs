
using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Axtae.Encode;

using static System.Text.Encoding;

namespace Axtae;

/// <summary>
/// Provides a high‑performance, asynchronous logging system with bounded
/// buffering.
/// </summary>
/// <remarks>
/// <para>
/// Log entries are written asynchronously to a file in the temporary
/// directory.
/// The logger uses a bounded channel (<see cref="IChannel{T}"/>) to decouple
/// producers (log calls) from the consumer (background writer).
/// </para>
/// <para>
/// The background task writes entries in batches, ensuring minimal impact on
/// calling threads. Each log entry is formatted with a timestamp, level,
/// source file, member name, line number, and the message.
/// </para>
/// <para>
/// All <see langword="static"/> methods are thread‑safe. The logger can be
/// closed by calling <see cref="Complete"/>.
/// </para>
/// </remarks>
public static class Logger {
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
  /// The maximum length of a single formatted log entry, in bytes.
  /// </summary>
  /// <value>4 KiB.</value>
  public const uint MaxEntryLength = 4 * Numeric.KiB;

  /// <summary>
  /// The capacity of the internal channel (number of buffered log entries).
  /// </summary>
  /// <value>128 entries.</value>
  public const uint Size = 128;

  /// <summary>
  /// The full path to the log file.
  /// </summary>
  /// <remarks>
  /// Located in <c>%TEMP%/GO/x.log</c>. The directory is created
  /// automatically.
  /// </remarks>
  public static readonly string LogFilePath
    = Path.Combine(Path.GetTempPath(), "Axtae", "x.log");

  // The bounded channel that holds pending log entries.
  private static readonly IChannel<LogEntry> _channel
    = Channel.CreateBounded<LogEntry>(Size);

  /// <summary>
  /// Initializes the logger and starts the background writing task.
  /// </summary>
  /// <remarks>
  /// The background task continuously reads from the channel, formats each
  /// entry, and writes it to the log file. It writes in batches when possible.
  /// The task runs until the channel is completed.
  /// </remarks>
  static Logger() {
    _ = Task.Run(static async () => {
      try {
        byte[] buffer = new byte[(int)MaxEntryLength];
        var encoder = UTF8.GetEncoder();
        var channel = _channel;
        var reader = channel.Reader;

        while (true) {
          var entry = await reader.ReadAsync()
            .ConfigureAwait(false);

          var span = buffer.AsSpan();
          Write(span, entry, encoder);

          // Drain any additional entries that may have arrived while
          // we were formatting the first one.
          while (reader.TryRead(out var item))
            Write(span, item, encoder);
        }
      }
      finally {
        // Ensure the file writer is disposed when the background task ends.
        FileWriter.Writer.Dispose();
      }

#pragma warning disable RCS1242 // Do not pass non-read-only struct by
      // read-only reference
      static void Write(Span<byte> span, in LogEntry entry, System.Text.Encoder encoder) {
        uint len = Parse(entry, span, encoder);

        FileWriter.Writer.Write(span[..(int)len]);

        if (entry.Level is Level.Error) FileWriter.Writer.Flush();
      }
#pragma warning restore RCS1242 // Do not pass non-read-only struct by
      // read-only reference
    });
  }

  /// <summary>
  /// Represents a single log entry containing the message, metadata, and
  /// timestamp.
  /// </summary>
  /// <remarks>
  /// This struct is immutable after construction, except for the
  /// <see cref="Timestamp"/> field which is mutated by the logger.
  /// It is designed to be passed by <see langword="in"/> parameter to avoid
  /// copying.
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
  /// Logs an debug message (only compiled in DEBUG builds).
  /// </summary>
  /// <param name="msg">The message to log.</param>
  /// <param name="file">
  /// The source file path, automatically filled by the compiler.
  /// </param>
  /// <param name="member">
  /// The calling member name, automatically filled by the compiler.
  /// </param>
  /// <param name="line">
  /// The line number, automatically filled by the compiler.
  /// </param>
  /// <remarks>
  /// This method is a no‑op in RELEASE builds due to the
  /// <see cref="System.Diagnostics.ConditionalAttribute"/>.
  /// </remarks>
  [System.Diagnostics.Conditional("DEBUG")]
  public static void Debug(string msg,
    [CallerFilePath] string file = "",
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0
  ) => Log(Level.Debug, msg, file, member, line);

  /// <summary>
  /// Logs an informational message.
  /// </summary>
  /// <param name="msg">The message to log.</param>
  /// <param name="file">
  /// The source file path, automatically filled by the compiler.
  /// In DEBUG builds it is the actual path; otherwise a placeholder.
  /// </param>
  /// <param name="member">
  /// The calling member name, automatically filled by the compiler.
  /// In DEBUG builds it is the actual path; otherwise a placeholder.
  /// </param>
  /// <param name="line">
  /// The line number, automatically filled by the compiler.
  /// In DEBUG builds it is the actual line; otherwise -1.
  /// </param>
  public static void Info(string msg,
#if DEBUG
    [CallerFilePath] string file = "",
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0
#else
    string file = "",
    string member = "",
    int line = -1
#endif
  ) => Log(Level.Info, msg, file, member, line);

  /// <summary>
  /// Logs a warning message.
  /// </summary>
  /// <param name="msg">The message to log.</param>
  /// <param name="file">
  /// The source file path, automatically filled by the compiler.
  /// In DEBUG builds it is the actual path; otherwise a placeholder.
  /// </param>
  /// <param name="member">
  /// The calling member name, automatically filled by the compiler.
  /// In DEBUG builds it is the actual path; otherwise a placeholder.
  /// </param>
  /// <param name="line">
  /// The line number, automatically filled by the compiler.
  /// In DEBUG builds it is the actual line; otherwise -1.
  /// </param>
  public static void Warning(string msg,
#if DEBUG
    [CallerFilePath] string file = "",
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0
#else
    string file = "",
    string member = "",
    int line = -1
#endif
  ) => Log(Level.Warning, msg, file, member, line);

  /// <summary>
  /// Logs an error message.
  /// </summary>
  /// <param name="msg">The message to log.</param>
  /// <param name="member">
  /// The calling member name, automatically filled by the compiler.
  /// It will not trim with out DUBUG.
  /// </param>
  /// <param name="file">
  /// The source file path, automatically filled by the compiler.
  /// In DEBUG builds it is the actual path; otherwise a placeholder.
  /// </param>
  /// <param name="line">
  /// The line number, automatically filled by the compiler.
  /// In DEBUG builds it is the actual line; otherwise -1.
  /// </param>
  public static void Error(string msg,
    [CallerMemberName] string member = "",
#if DEBUG
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0
#else
    string file = "?",
    int line = -1
#endif
  ) => Log(Level.Error, msg, file, member, line);

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
  /// it falls back to an asynchronous write (fire‑and‑forget).
  /// </remarks>
  public static void Log(Level level, string msg,
    string file, string member, int line) {
    if (string.IsNullOrWhiteSpace(msg)
      || _channel.State is not Channel.Active) return;

    var entry = new LogEntry {
      Msg = msg,
      Level = level,
      File = file,
      Member = member,
      Line = line
    };

    TimeStamp.GetStamp(entry.Timestamp);

    var writer = _channel.Writer;
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
  public static void Complete() => _channel.Writer.Complete();

#pragma warning disable RCS1242 // Do not pass non-read-only struct by
  // read-only reference
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
  private static uint Parse(in LogEntry entry, Span<byte> span,
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

    if (!AddString(span, entry.File, ref len, encoder)) return (uint)len;

    if (!AddByte(span, Ascii.OpenParenthesis, ref len)) return (uint)len;

    byte l = entry.Line.ToAscii(span[len..]);
    if (l is not 0) len += l;
    else {
      AddDots(span, ref len);
      return (uint)len;
    }

    if (!AddBytes(span, ") --> "u8, ref len)) return (uint)len;

    if (!AddString(span, entry.Member, ref len, encoder)) return (uint)len;

    if (!AddByte(span, Ascii.LF, ref len)) return (uint)len;

    if (!AddString(span, entry.Msg, ref len, encoder)) return (uint)len;

    if (!AddByte(span, Ascii.LF, ref len)) return (uint)len;

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
#pragma warning restore RCS1242 // Do not pass non-read-only struct by
  // read-only reference

  /// <summary>
  /// Wraps a <see cref="FileStream"/> to write log entries to the log file
  /// and also writes a header with version and environment information on
  /// creation.
  /// </summary>
  /// <remarks>
  /// This class is a singleton (<see cref="Writer"/>) and is disposed when
  /// the background task completes. It is not thread‑safe; it is only used by
  /// the single background writer.
  /// </remarks>
  private sealed class FileWriter : IDisposable {
    /// <summary>The singleton instance of the file writer.</summary>
    public static readonly FileWriter Writer = new(LogFilePath);

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
      // Cover: null branch – never happens in practice
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
