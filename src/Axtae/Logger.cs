
using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Axtae.Encode;

using static System.Text.Encoding;

namespace Axtae;

public static class Logger {
  public enum Level { Debug = 1, Info = 2, Warning = 3, Error = 4 }
  public const uint MaxEntryLength = 4 * Numeric.KiB;

  public const uint Size = 128;

  public static readonly string LogFilePath
    = Path.Combine(Path.GetTempPath(), "GO", "x.log");

  private static readonly IChannel<LogEntry> _channel
    = Channel.CreateBounded<LogEntry>(Size);

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
          if (channel.State is Channel.Completed) return;

          var span = buffer.AsSpan();
          Write(span, entry, encoder);

          while (reader.TryRead(out var item))
            Write(span, item, encoder);
        }
      }
      finally {
        FileWriter.Writer.Dispose();
      }

#pragma warning disable RCS1242 // Do not pass non-read-only struct by read-only reference
      static void Write(Span<byte> span, in LogEntry entry, System.Text.Encoder encoder) {
        uint len = Parse(entry, span, encoder);

        FileWriter.Writer.Write(span[..(int)len]);

        if (entry.Level is Level.Error) FileWriter.Writer.Flush();
      }
#pragma warning restore RCS1242 // Do not pass non-read-only struct by read-only reference
    });
  }

#pragma warning disable S1104
  public struct LogEntry {
    public string Msg { readonly get; init; }
    public string File { readonly get; init; }
    public string Member { readonly get; init; }
    public TimeStamp.Buffer Timestamp;
    public int Line { readonly get; init; }
    public Level Level { readonly get; init; }
  }
#pragma warning restore S1104

  [System.Diagnostics.Conditional("DEBUG")]
  public static void Debug(string msg,
#if DEBUG
    [CallerFilePath] string file = "",
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0
#else
    string file = "?",
    string member = "?",
    int line = -1
#endif
  ) => Log(Level.Debug, msg, file, member, line);

  public static void Info(string msg,
#if DEBUG
    [CallerFilePath] string file = "",
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0
#else
    string file = "?",
    string member = "?",
    int line = -1
#endif
  ) => Log(Level.Info, msg, file, member, line);

  public static void Warning(string msg,
#if DEBUG
    [CallerFilePath] string file = "",
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0
#else
    string file = "?",
    string member = "?",
    int line = -1
#endif
  ) => Log(Level.Warning, msg, file, member, line);

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

  public static void Complete() => _channel.Writer.Complete();

#pragma warning disable RCS1242 // Do not pass non-read-only struct by read-only reference
  private static uint Parse(in LogEntry entry, Span<byte> span,
    System.Text.Encoder encoder) {
    System.Diagnostics.Debug.Assert(span.Length == MaxEntryLength);
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
#pragma warning restore RCS1242 // Do not pass non-read-only struct by read-only reference

  private sealed class FileWriter : IDisposable {
    public static readonly FileWriter Writer
      = new(LogFilePath);

    private readonly FileStream _stream;

    private FileWriter(string file_path) {
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
      var version = typeof(Logger).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion;

      stream.Write("Version:\n"u8);
      stream.Write("  Logger: "u8);
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

    public void Write(ReadOnlySpan<byte> what) => this._stream.Write(what);

    public void Flush() => this._stream.Flush();

    public void Dispose() => this._stream.Dispose();
  }
}
