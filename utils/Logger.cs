
#if DebugLog

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

using Utils.Encode;

namespace Utils;

public sealed class Logger {
  public enum Level { Debug, Info, Warning, Error }
  public const int MaxEntryLength = 1024;

  private static readonly IChannelWriter<LogEntry> _writer;
  private static readonly IChannelReader<LogEntry> _reader;

  static Logger() {
    var _channel = Channel.CreateBounded<LogEntry>(128);
    _writer = _channel.Writer;
    _reader = _channel.Reader;
  }

  public struct LogEntry {
    public string msg;
    public string file;
    public string member;
    public TimestampBuffer timestamp;
    public int line;
    public Level level;

    [InlineArray(TimeStamp.Size)]
    public struct TimestampBuffer {
      private byte _element;
    }
  }

  public static async void Log(
    string msg,
    Level level,
    [CallerFilePath] string file = "",
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0) {
    if (string.IsNullOrWhiteSpace(msg)) return;

    var entry = new LogEntry {
      msg = msg,
      level = level,
      file = file,
      member = member,
      line = line
    };

    TimeStamp.GetStamp(entry.timestamp);
    if (_writer.TryWrite(entry)) return;
    try {
      await _writer.WriteAsync(entry)
        .ConfigureAwait(false);
    }
    catch (Exception) { }
  }

  private static async void Write(uint how_many) {
    Memory<byte> buffer = new byte[MaxEntryLength];
    while (how_many > 0) {
      try {
        var entry = await _reader.ReadAsync()
          .ConfigureAwait(false);
        if (entry.msg is null) return;

        Prase(entry, buffer.Span);

        FileWriter.Writer.Write(buffer.Span);
      }
      catch (Exception) { }
      how_many--;
    }
  }

  private static void Prase(LogEntry entry, Span<byte> buffer) {

  }
}

public sealed class TimeStamp {
  public const uint TTL = 10;
  public const byte Size = 22;

  private static readonly byte[] LUT = new byte[200];

  private static readonly Lock _lock = new();
  private static readonly byte[] _cache = new byte[22];
  private static long _stamp = 0;

  static TimeStamp() {
    for (int i = 0; i < 100; i++) {
      int idx = i * 2;
      (int q, int r) = Math.DivRem(i, 10);
      LUT[idx] = (byte)(Ascii.Zero + q);
      LUT[idx + 1] = (byte)(Ascii.Zero + r);
    }

    _cache[0] = Ascii.Two;
    _cache[1] = Ascii.Zero;
    _cache[4] = Ascii.HyphenMinus;
    _cache[7] = Ascii.HyphenMinus;
    _cache[10] = Ascii.Space;
    _cache[13] = Ascii.Colon;
    _cache[16] = Ascii.Colon;
    _cache[19] = Ascii.Period;
  }

  public static void GetStamp(Span<byte> span) {
    if (span.Length < 22) return;

    long now = Environment.TickCount64;
    if (now - Volatile.Read(ref _stamp) < TTL) {
      _cache.CopyTo(span);
      return;
    }

    lock (_lock) {
      if (now - Volatile.Read(ref _stamp) < TTL) {
        _cache.CopyTo(span);
        return;
      }

      UpdateCache();

      _stamp = Environment.TickCount64;
      _cache.CopyTo(span);
    }
  }

  private static void UpdateCache() {
    DateTime now = DateTime.UtcNow;

    int year = (now.Year - 2000) * 2;
    _cache[2] = LUT[year];
    _cache[3] = LUT[year + 1];

    int month = now.Month * 2;
    _cache[5] = LUT[month];
    _cache[6] = LUT[month + 1];

    int day = now.Day * 2;
    _cache[8] = LUT[day];
    _cache[9] = LUT[day + 1];

    int hour = now.Hour * 2;
    _cache[11] = LUT[hour];
    _cache[12] = LUT[hour + 1];

    int minute = now.Minute * 2;
    _cache[14] = LUT[minute];
    _cache[15] = LUT[minute + 1];

    int second = now.Second * 2;
    _cache[17] = LUT[second];
    _cache[18] = LUT[second + 1];

    int millisecond = now.Millisecond / 10 * 2;
    _cache[20] = LUT[millisecond];
    _cache[21] = LUT[millisecond + 1];
  }
}

public sealed class FileWriter : IDisposable {
  public const byte NewLine = 10;

  public static readonly FileWriter Writer
    = new(Path.Combine(Path.GetTempPath(), "GO", "x.log"));

  private readonly FileStream _stream;
  private readonly Lock _lock = new();

  private FileWriter(string filePath) {
    this._stream = new(filePath,
      FileMode.Append,
      FileAccess.Write,
      FileShare.Read,
      8 * 1024);
  }

  public void Write(ReadOnlySpan<byte> what) {
    lock (this._lock) {
      this._stream.Write(what);
    }
  }

  public void Dispose() {
    this._stream.Dispose();
  }
}

#endif
