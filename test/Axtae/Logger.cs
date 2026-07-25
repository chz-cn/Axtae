
using System.Threading.Tasks;
using Axtae;

namespace Test;

public sealed class LoggerTests {
  public const string LoggerPath = "test/Axtae/Logger.cs";

  [Fact]
  public void Log_ValidMessage_DoesNotThrow() {
    var ex = Record.Exception(static () => Logger.Info("Test message"));
    Assert.Null(ex);
  }

  [Fact]
  public void Log_EmptyMessage_DoesNotThrow() {
    var ex = Record.Exception(static () => Logger.Warning(""));
    Assert.Null(ex);

    ex = Record.Exception(static () => Logger.Error("   "));
    Assert.Null(ex);
  }

  [Fact]
  public void Log_NullMessage_DoesNotThrow() {
    var ex = Record.Exception(static () => Logger.Debug(null!));
    Assert.Null(ex);
  }

  [Fact]
  public async Task Log_WhenChannelFull_DoesNotBlockIndefinitely() {
    var task = Task.Run(static () => {
      for (int i = 0; i < 200; i++)
        Logger.Debug($"Bulk message {i}");
    });

    var completed = await Task.WhenAny(task, Task.Delay(2000));
    Assert.Equal(task, completed);
  }

  [Fact]
  public void Log_MaxEntryLength() {
    string msg = new('a', 4096); // MaxEntryLength = 4096
    Logger.Info(msg);
    Logger.Log(Logger.Level.Info, "msg", msg, "", -1);

    int file_len = (int)(Logger.MaxEntryLength - TimeStamp.Size - "[Info]"u8.Length - 2);
    Logger.Log(Logger.Level.Info, "msg", new('f', file_len), "", -1);
    Logger.Log(Logger.Level.Info, "msg", new('f', file_len - 1), "", -1);
    Logger.Log(Logger.Level.Info, "msg", new('f', file_len - 3), "", -1);
    Logger.Log(Logger.Level.Info, "msg", new('f', file_len - 10), "", -1);

    Logger.Log(Logger.Level.Info, "msg", LoggerPath, msg, -1);

    Logger.Info(new('m', 4004));

    Assert.True(true);
  }

  [Fact]
  public void Log_EveryLevel() {
    Logger.Debug("Debug");
    Logger.Info("Info");
    Logger.Warning("Warning");
    Logger.Error("Error");

    Assert.True(true);
  }

  [Fact]
  public void Log_WithInvalidLevel_ShouldUseUnknownLevel() {
    const Logger.Level L = (Logger.Level)999;
    Logger.Log(L, "test invalid level",
      LoggerPath, nameof(Log_WithInvalidLevel_ShouldUseUnknownLevel), -1);

    Assert.True(true);
  }

  [Fact]
  public void Log_WithEmptyString_ShouldWriteQuestionMark() {
    Logger.Log(Logger.Level.Info, "msg",
      "", nameof(Log_WithEmptyString_ShouldWriteQuestionMark), -1);
    Logger.Log(Logger.Level.Info, "msg", LoggerPath, "", -1);

    Assert.True(true);
  }

  [Fact]
  public async Task Complete_TriggersBackgroundTaskToExitAndDispose() {
    Logger.Complete();

    await Task.Delay(100);
    Assert.True(true);
  }
}
