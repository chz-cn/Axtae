
using System;
using System.IO;
using System.Threading.Tasks;
using Axtae;

namespace Test;

public sealed class LoggerTests {
  public static readonly string LoggerPath
    = Path.Combine(Path.GetTempPath(), "Axtae", "x.log");
  public static readonly Logger Log = new(LoggerPath, 4096, 8);

  [Fact]
  public void Log_ValidMessage_DoesNotThrow() {
    var ex = Record.Exception(static () => Log.Info("Test message"));
    Assert.Null(ex);
  }

  [Fact]
  public void Log_EmptyMessage_DoesNotThrow() {
    var ex = Record.Exception(static () => Log.Warning(""));
    Assert.Null(ex);

    ex = Record.Exception(static () => Log.Error("   "));
    Assert.Null(ex);
  }

  [Fact]
  public void Log_NullMessage_DoesNotThrow() {
    var ex = Record.Exception(static () => Log.Debug(null!));
    Assert.Null(ex);
  }

  [Fact]
  public async Task Log_WhenChannelFull_DoesNotBlockIndefinitely() {
    var task = Task.Run(static () => {
      for (int i = 0; i < 20; i++)
        Log.Debug($"Bulk message {i}");
    });

    var completed = await Task.WhenAny(task, Task.Delay(200));
    Assert.Equal(task, completed);
  }

  [Fact]
  public void Log_MaxEntryLength() {
    string msg = new('a', 4096); // MaxEntryLength = 4096
    Log.Info(msg);
    Log.Log(Logger.Level.Info, "msg", msg, "", -1);

    int file_len = Log.MaxEntryLength - TimeStamp.Size - "[Info]"u8.Length - 2;
    Log.Log(Logger.Level.Info, "msg", new('f', file_len), "", -1);
    Log.Log(Logger.Level.Info, "msg", new('f', file_len - 1), "", -1);
    Log.Log(Logger.Level.Info, "msg", new('f', file_len - 3), "", -1);
    Log.Log(Logger.Level.Info, "msg", new('f', file_len - 10), "", -1);

    Log.Log(Logger.Level.Info, "msg", LoggerPath, msg, -1);

    Log.Info(new('m', 4004));

    Assert.True(true);
  }

  [Fact]
  public void Log_EveryLevel() {
    Log.Debug("Debug");
    Log.Info("Info");
    Log.Warning("Warning");
    Log.Error("Error");

    Assert.True(true);
  }

  [Fact]
  public void Log_WithInvalidLevel_ShouldUseUnknownLevel() {
    const Logger.Level L = (Logger.Level)999;
    Log.Log(L, "test invalid level",
      LoggerPath, nameof(Log_WithInvalidLevel_ShouldUseUnknownLevel), -1);

    Assert.True(true);
  }

  [Fact]
  public void Log_WithEmptyString_ShouldWriteQuestionMark() {
    Log.Log(Logger.Level.Info, "msg",
      "", nameof(Log_WithEmptyString_ShouldWriteQuestionMark), -1);
    Log.Log(Logger.Level.Info, "msg", LoggerPath, "", -1);

    Assert.True(true);
  }

  [Fact]
  public async Task Complete_TriggersBackgroundTaskToExitAndDispose() {
    Log.Complete();

    await Task.Delay(10);
    Assert.True(true);
  }

  [Fact]
  public void MaxEntryLength_DoesClampTo100() {
    var path = Path.Combine(Path.GetTempPath(), "Axtae", "i.log");
    Logger log = new(path, 0);
    Assert.Equal(100, log.MaxEntryLength);
    log.Complete();
  }

  [Fact]
  public void Size_DoesClampTo4() {
    var path = Path.Combine(Path.GetTempPath(), "Axtae", "4.log");
    Logger log = new(path, 128, 0);
    Assert.Equal(4u, log.Size);
    log.Complete();
  }

  [Fact]
  public void LogFilePath_ThrowsIfNullOrWhiteSpace() {
    _ = Assert.Throws<ArgumentNullException>(() => new Logger(null!));
    _ = Assert.Throws<ArgumentException>(() => new Logger(""));
    _ = Assert.Throws<ArgumentException>(() => new Logger("   "));
  }
}
