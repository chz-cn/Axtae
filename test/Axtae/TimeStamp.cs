// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Axtae;

using Xunit;

namespace Test;

public static class TimeStampTests {
  public const int ExpectedLength = TimeStamp.Size;

  [Fact]
  public static void GetStamp_ReturnsCorrectLengthAndFormat () {
    Span<byte> buffer = stackalloc byte[ExpectedLength];
    TimeStamp.GetStamp (buffer);

    string stamp = Encoding.ASCII.GetString (buffer);
    Assert.Equal (ExpectedLength, stamp.Length);

    Assert.Equal ('-', stamp[4]);
    Assert.Equal ('-', stamp[7]);
    Assert.Equal ('T', stamp[10]);
    Assert.Equal (':', stamp[13]);
    Assert.Equal (':', stamp[16]);
    Assert.Equal ('.', stamp[19]);
    Assert.Equal ('Z', stamp[22]);

    var pvd = CultureInfo.InvariantCulture;

    int year = int.Parse (stamp[0..4], pvd);
    Assert.InRange (year, 2000, 9999);

    int month = int.Parse (stamp[5..7], pvd);
    Assert.InRange (month, 1, 12);
    int day = int.Parse (stamp[8..10], pvd);
    Assert.InRange (day, 1, 31);
    int hour = int.Parse (stamp[11..13], pvd);
    Assert.InRange (hour, 0, 23);
    int minute = int.Parse (stamp[14..16], pvd);
    Assert.InRange (minute, 0, 59);
    int second = int.Parse (stamp[17..19], pvd);
    Assert.InRange (second, 0, 59);
    int millis = int.Parse (stamp[20..22], pvd);
    Assert.InRange (millis, 0, 99);
  }

  [Fact]
  public static void GetStamp_CacheHit_ReturnsSameContentWithinTTL () {
    Span<byte> first = stackalloc byte[ExpectedLength];
    Span<byte> second = stackalloc byte[ExpectedLength];

    TimeStamp.GetStamp (first);

    TimeStamp.GetStamp (second);

    Assert.True (first.SequenceEqual (second));
  }

  [Fact]
  public static async Task GetStamp_CacheExpires_AfterTTL () {
    byte[] first = new byte[ExpectedLength];
    byte[] second = new byte[ExpectedLength];

    TimeStamp.GetStamp (first);
    await Task.Delay (20, TestContext.Current.CancellationToken);
    TimeStamp.GetStamp (second);

    string s1 = Encoding.ASCII.GetString (first);
    string s2 = Encoding.ASCII.GetString (second);
    Assert.NotEqual (s1, s2);
  }

  [Fact]
  public static Task GetStamp_ConcurrentCalls_NoCorruption () {
    const int Iterations = 1000;

    InlineArray2<Task> arr = default;

    foreach (ref var t in arr) {
      t = Task.Factory.StartNew (
        Executor,
        TestContext.Current.CancellationToken,
        TaskCreationOptions.RunContinuationsAsynchronously,
        TaskScheduler.Default
      );

      static void Executor () {
        var pvd = CultureInfo.InvariantCulture;
        Span<byte> buf = stackalloc byte[ExpectedLength];
        for (int i = 0; i < Iterations; i++) {
          TimeStamp.GetStamp (buf);
          string s2 = Encoding.ASCII.GetString (buf);
          Assert.True (DateTime.TryParse (s2, pvd, out _));
        }
      }
    }

    return Task.WhenAll (arr);
  }

  [Fact]
  public static void GetStamp_BufferTooShort_DoesNothing () {
    Span<byte> insufficient = stackalloc byte[10];

    TimeStamp.GetStamp (insufficient);

    Span<byte> buffer = stackalloc byte[10];
    Assert.True (insufficient.SequenceEqual (buffer));
  }

  [Fact]
  public static void GetStamp_ExactLength_FillsBuffer () {
    Span<byte> buffer = stackalloc byte[ExpectedLength];
    TimeStamp.GetStamp (buffer);

    Assert.NotEqual (0, buffer[ExpectedLength - 1]);
  }
}
