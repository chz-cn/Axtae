
#if DebugLog

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Utils;

public static class Logger {
  public enum Level { Debug, Info, Warning, Error }

  public static void Log(
    ReadOnlySpan<char> msg,
    Level level,
    [CallerFilePath] string file = "",
    [CallerMemberName] string member = "",
    [CallerLineNumber] int line = 0) {
    if (msg.IsEmpty) return;

    Span<byte> span = stackalloc byte[msg.Length];

    byte start = 0;
    ReadOnlySpan<byte> prefix = level switch {
      Level.Debug => "[Debug]"u8,
      Level.Info => "[Info]"u8,
      Level.Warning => "[Warning]"u8,
      Level.Error => "[Error]"u8,
      _ => null
    };
    prefix.CopyTo(span);
    start += (byte)prefix.Length;

  }
}

public class FileWriter : IDisposable {
  public const byte NewLine = 10;

  public static readonly FileWriter Writer
    = new(Path.Combine(Path.GetTempPath(), "GO", "x.log"));

  private readonly FileStream _stream;

  private FileWriter(string filePath) {
    this._stream = new(filePath,
      FileMode.Append,
      FileAccess.Write,
      FileShare.Read,
      4096);
  }

  public void Write(ReadOnlySpan<byte> what) => this._stream.Write(what);

  public void Dispose() => this._stream.Dispose();
}

#endif
