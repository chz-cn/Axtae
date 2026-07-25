
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Axtae.Encode;

namespace Axtae;

/// <summary>
/// Provides high-performance timestamp generation with caching for log entries.
/// </summary>
/// <remarks>
/// Timestamps are formatted as "YY-MM-DD HH:mm:ss.ff" (22 bytes) and cached for
/// <see cref="TTL"/> milliseconds to avoid repeated formatting overhead.
/// </remarks>
public static class TimeStamp {
  /// <summary>
  /// The time-to-live in milliseconds for cached timestamp values.
  /// </summary>
  /// <remarks>
  /// A cached timestamp is reused for up to 10 milliseconds before being refreshed.
  /// </remarks>
  public const uint TTL = 10;

  /// <summary>
  /// The size in bytes of the formatted timestamp buffer (22 bytes).
  /// Format: "YY-MM-DD HH:mm:ss.ff" plus null terminator handling.
  /// </summary>
  public const byte Size = 22;

  private static readonly Lock _lock = new();

  /// <summary>
  /// Represents a fixed-size buffer for storing a formatted timestamp.
  /// </summary>
  /// <remarks>
  /// This struct uses <see cref="InlineArrayAttribute"/> with <see cref="Size"/> elements.
  /// It can be treated as a <see cref="Span{T}"/> of bytes for direct manipulation.
  /// </remarks>
  [InlineArray(Size)]
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
#pragma warning disable S1104 // Fields should not have public accessibility
  public struct Buffer { public byte V; }
#pragma warning restore S1104 // Fields should not have public accessibility
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

  private static Buffer _cache = new();
  private static long _stamp = 0;

  static TimeStamp() {
    _cache[0] = Ascii.Two;
    _cache[1] = Ascii.Zero;
    _cache[4] = Ascii.HyphenMinus;
    _cache[7] = Ascii.HyphenMinus;
    _cache[10] = Ascii.Space;
    _cache[13] = Ascii.Colon;
    _cache[16] = Ascii.Colon;
    _cache[19] = Ascii.Period;
  }

  /// <summary>
  /// Writes the current timestamp (formatted as "YY-MM-DD HH:mm:ss.ff") into the provided span.
  /// </summary>
  /// <param name="span">The destination span that must be at least <see cref="Size"/> bytes in length.</param>
  /// <remarks>
  /// If the timestamp has been recently generated (within <see cref="TTL"/> milliseconds),
  /// the cached value is returned. Otherwise, the cache is refreshed with the current UTC time.
  /// This method is thread-safe.
  /// </remarks>
  public static void GetStamp(Span<byte> span) {
    if (span.Length < 22) return;

    long now = Environment.TickCount64;
    if (now - Volatile.Read(ref _stamp) < TTL) {
      ((Span<byte>)_cache).CopyTo(span);
      return;
    }

    lock (_lock) {
      if (now - Volatile.Read(ref _stamp) < TTL) {
        ((Span<byte>)_cache).CopyTo(span);
        return;
      }

      UpdateCache();

      _stamp = Environment.TickCount64;
      ((Span<byte>)_cache).CopyTo(span);
    }
  }

  private static void UpdateCache() {
    DateTime now = DateTime.UtcNow;
    var LUT = Ascii.TwoDigit;

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
