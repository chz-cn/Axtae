// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.CompilerServices;
using System.Threading;

using Axtae.Codecs;

namespace Axtae;

/// <summary>
/// Provides high-performance UTC timestamp generation for log entries.
/// </summary>
/// <remarks>
/// The most recent <see cref="DateTime"/> value is cached for up to
/// <see cref="TTL"/> milliseconds to avoid repeated system time reads.
/// Formatting is performed on every call and writes directly into the
/// caller-supplied buffer.
/// </remarks>
public static class TimeStamp {
  /// <summary>
  /// The time-to-live in milliseconds for the cached <see cref="DateTime"/>.
  /// </summary>
  /// <remarks>
  /// The cached value is reused for up to 10 milliseconds before being
  /// refreshed. Effective resolution is bounded by
  /// <see cref="Environment.TickCount64"/>.
  /// </remarks>
  public const uint TTL = 10;

  /// <summary>
  /// The size in bytes of a formatted timestamp.
  /// </summary>
  /// <remarks>
  /// Format: "YYYY-MM-DDTHH:mm:ss.ffZ" (23 bytes, no null terminator).
  /// </remarks>
  public const byte Size = 23;

  private static long _ticks;
  private static long _stamp;

  /// <summary>
  /// Gets the current UTC time, refreshing the cache at most once every
  /// <see cref="TTL"/> milliseconds.
  /// </summary>
  /// <remarks>
  /// This is a low-overhead alternative to <see cref="DateTime.UtcNow"/>
  /// for call sites that can tolerate up to <see cref="TTL"/> milliseconds
  /// of staleness. Use <see cref="DateTime.UtcNow"/> directly if an exact
  /// reading is required.
  /// </remarks>
  public static DateTime UtcNow {
    get {
      long tick = Environment.TickCount64;
      long stamp = Volatile.Read (ref _stamp);
      DateTime now;
      if (tick - stamp < TTL) {
        // Acquire on _stamp makes the _ticks write visible.
        now = new (_ticks, DateTimeKind.Utc);
      }
      else {
        now = DateTime.UtcNow;
        Volatile.Write (ref _ticks, now.Ticks);
        Volatile.Write (ref _stamp, tick);
      }

      return now;
    }
  }

  /// <summary>
  /// Formats the current UTC timestamp into <paramref name="span"/>.
  /// </summary>
  /// <param name="span">
  /// The destination buffer. Must be at least <see cref="Size"/> bytes;
  /// otherwise the method returns without writing anything.
  /// </param>
  /// <remarks>
  /// Format: "YYYY-MM-DDTHH:mm:ss.ffZ".
  /// The underlying <see cref="DateTime"/> is obtained from
  /// <see cref="UtcNow"/> and is refreshed at most once every
  /// <see cref="TTL"/> milliseconds.
  /// </remarks>
  public static void GetStamp (Span<byte> span) {
    if (span.Length < Size) {
      return;
    }

    Format (UtcNow, ref span.GetPinnableReference ());
  }

  /// <summary>
  /// Writes the formatted representation of <paramref name="time"/> into
  /// <paramref name="start"/>.
  /// </summary>
  /// <param name="time">The value to format. Expected to be UTC.</param>
  /// <param name="start">
  /// A reference to the first byte of the destination buffer. Must point
  /// to at least <see cref="Size"/> writable bytes.
  /// </param>
  public static void Format (DateTime time, scoped ref byte start) {
    ref byte LUT = ref Unsafe.AsRef
      (in Ascii.TwoDigit.GetPinnableReference ());

    var (century, year) = Math.DivRem (time.Year, 100);

    century *= 2;
    start = Unsafe.Add (ref LUT, century);
    Unsafe.Add (ref start, 1) = Unsafe.Add (ref LUT, century + 1);

    year *= 2;
    Unsafe.Add (ref start, 2) = Unsafe.Add (ref LUT, year);
    Unsafe.Add (ref start, 3) = Unsafe.Add (ref LUT, year + 1);
    Unsafe.Add (ref start, 4) = Ascii.HyphenMinus;

    int month = time.Month * 2;
    Unsafe.Add (ref start, 5) = Unsafe.Add (ref LUT, month);
    Unsafe.Add (ref start, 6) = Unsafe.Add (ref LUT, month + 1);
    Unsafe.Add (ref start, 7) = Ascii.HyphenMinus;

    int day = time.Day * 2;
    Unsafe.Add (ref start, 8) = Unsafe.Add (ref LUT, day);
    Unsafe.Add (ref start, 9) = Unsafe.Add (ref LUT, day + 1);
    Unsafe.Add (ref start, 10) = Ascii.T;

    int hour = time.Hour * 2;
    Unsafe.Add (ref start, 11) = Unsafe.Add (ref LUT, hour);
    Unsafe.Add (ref start, 12) = Unsafe.Add (ref LUT, hour + 1);
    Unsafe.Add (ref start, 13) = Ascii.Colon;

    int minute = time.Minute * 2;
    Unsafe.Add (ref start, 14) = Unsafe.Add (ref LUT, minute);
    Unsafe.Add (ref start, 15) = Unsafe.Add (ref LUT, minute + 1);
    Unsafe.Add (ref start, 16) = Ascii.Colon;

    int second = time.Second * 2;
    Unsafe.Add (ref start, 17) = Unsafe.Add (ref LUT, second);
    Unsafe.Add (ref start, 18) = Unsafe.Add (ref LUT, second + 1);
    Unsafe.Add (ref start, 19) = Ascii.Period;

    int millisecond = time.Millisecond / 10 * 2;
    Unsafe.Add (ref start, 20) = Unsafe.Add (ref LUT, millisecond);
    Unsafe.Add (ref start, 21) = Unsafe.Add (ref LUT, millisecond + 1);
    Unsafe.Add (ref start, 22) = Ascii.Z;
  }
}
