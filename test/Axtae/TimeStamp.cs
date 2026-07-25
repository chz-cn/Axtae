
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Axtae;

namespace Test;

public sealed class TimeStampTests {
  private const int ExpectedLength = TimeStamp.Size;

  [Fact]
  public void GetStamp_ReturnsCorrectLengthAndFormat() {
    Span<byte> buffer = stackalloc byte[ExpectedLength];
    TimeStamp.GetStamp(buffer);

    string stamp = Encoding.UTF8.GetString(buffer);
    Assert.Equal(ExpectedLength, stamp.Length);

    Assert.Equal('-', stamp[4]);
    Assert.Equal('-', stamp[7]);
    Assert.Equal(' ', stamp[10]);
    Assert.Equal(':', stamp[13]);
    Assert.Equal(':', stamp[16]);
    Assert.Equal('.', stamp[19]);

    int year = int.Parse(stamp[0..4]);
    Assert.InRange(year, 2000, 2099);

    int month = int.Parse(stamp[5..7]);
    Assert.InRange(month, 1, 12);
    int day = int.Parse(stamp[8..10]);
    Assert.InRange(day, 1, 31);
    int hour = int.Parse(stamp[11..13]);
    Assert.InRange(hour, 0, 23);
    int minute = int.Parse(stamp[14..16]);
    Assert.InRange(minute, 0, 59);
    int second = int.Parse(stamp[17..19]);
    Assert.InRange(second, 0, 59);
    int millis = int.Parse(stamp[20..22]);
    Assert.InRange(millis, 0, 99);
  }

  [Fact]
  public void GetStamp_CacheHit_ReturnsSameContentWithinTTL() {
    Span<byte> first = stackalloc byte[ExpectedLength];
    Span<byte> second = stackalloc byte[ExpectedLength];

    TimeStamp.GetStamp(first);

    TimeStamp.GetStamp(second);

    Assert.True(first.SequenceEqual(second));
  }

  [Fact]
  public async Task GetStamp_CacheExpires_AfterTTL() {
    byte[] first = new byte[ExpectedLength];
    byte[] second = new byte[ExpectedLength];

    TimeStamp.GetStamp(first);
    await Task.Delay(20);
    TimeStamp.GetStamp(second);

    string s1 = Encoding.UTF8.GetString(first);
    string s2 = Encoding.UTF8.GetString(second);
    Assert.NotEqual(s1, s2);
  }

  [InlineArray(ExpectedLength)]
  public struct Buf { public byte V; };

  [Fact]
  public static void GetStamp_ConcurrentCalls_NoCorruption() {
    const int Iterations = 1000;
    var results = new Buf[Iterations];

    _ = Parallel.For(0, Iterations, i => TimeStamp.GetStamp(results[i]));

    foreach (var b in results) {
      string s = Encoding.UTF8.GetString(b);
      Assert.Equal('-', s[4]);
      Assert.Equal(' ', s[10]);
    }

    foreach (var b in results) {
      string s = Encoding.UTF8.GetString(b);

      for (int i = 0; i < s.Length; i++) {
        char c = s[i];
        if (i is 4 or 7 or 10 or 13 or 16 or 19) continue;
        Assert.True(char.IsDigit(c), $"Non-digit at position {i}: '{c}'");
      }
    }
  }

  [Fact]
  public void GetStamp_BufferTooShort_DoesNothing() {
    Span<byte> shortBuffer = stackalloc byte[10];

    TimeStamp.GetStamp(shortBuffer);

    Assert.True(shortBuffer.SequenceEqual(new byte[10]));
  }

  [Fact]
  public void GetStamp_ExactLength_FillsBuffer() {
    Span<byte> buffer = stackalloc byte[ExpectedLength];
    TimeStamp.GetStamp(buffer);

    Assert.NotEqual(0, buffer[ExpectedLength - 1]);
  }

  [Fact]
  public async Task GetStamp_Free_DoubleCheck_ConcurrentRace() {
    const int Iterations = 1000;

    var tasks = new InlineArray10<Task>();

    var buffer = new Buf();

    for (int attempt = 0; attempt < Iterations; attempt++) {
      foreach (ref var task in tasks)
        task = Task.Run(() => TimeStamp.GetStamp(buffer));

      await Task.WhenAll(tasks);
    }

    Assert.True(true);
  }
}
