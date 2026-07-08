
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;

namespace Test;

public sealed class LoggerTests {
  [Fact]
  public async Task Log_ValidMessage_DoesNotThrow() {
    var ex = await Record.ExceptionAsync(static async () =>
      Logger.Log(Logger.Level.Info, "Test message")
    );
    Assert.Null(ex);
  }

  [Fact]
  public async Task Log_EmptyMessage_DoesNotThrow() {
    var ex = await Record.ExceptionAsync(static async () =>
      Logger.Log(Logger.Level.Warning, ""));
    Assert.Null(ex);

    ex = await Record.ExceptionAsync(static async () =>
      Logger.Log(Logger.Level.Error, "   "));
    Assert.Null(ex);
  }

  [Fact]
  public async Task Log_NullMessage_DoesNotThrow() {
    var ex = await Record.ExceptionAsync(static async () =>
      Logger.Log(Logger.Level.Debug, null!));
    Assert.Null(ex);
  }

  [Fact]
  public async Task Log_WhenChannelFull_DoesNotBlockIndefinitely() {
    var task = Task.Run(() => {
      for (int i = 0; i < 200; i++) {
        Logger.Log(Logger.Level.Debug, $"  Bulk message {i}");
      }
    });

    var completed = await Task.WhenAny(task, Task.Delay(2000));
    Assert.Equal(task, completed);
    await task;
  }

  [Fact]
  public void Log_MaxEntryLength() {
    Logger.Log(Logger.Level.Error, new('a', 4096));
  }

  [Fact]
  public void Log_EveryLevel() {
    Logger.Log(Logger.Level.Debug, "Debug");
    Logger.Log(Logger.Level.Info, "Info");
    Logger.Log(Logger.Level.Warning, "Warning");
    Logger.Log(Logger.Level.Error, "Error");
  }
}

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

  [Fact]
  public void GetStamp_ConcurrentCalls_NoCorruption() {
    const int iterations = 1000;
    var results = new byte[iterations][];

    Parallel.For(0, iterations, i => {
      var buffer = new byte[ExpectedLength];
      TimeStamp.GetStamp(buffer);
      results[i] = buffer;
    });

    foreach (var b in results) {
      Assert.Equal(ExpectedLength, b.Length);
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
}
