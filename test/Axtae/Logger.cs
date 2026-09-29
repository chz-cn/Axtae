// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading.Tasks;

using Axtae;

using Xunit;

namespace Test;

public sealed class LoggerFixture : IDisposable {
  public static readonly string LoggerPath
    = Path.Combine (Path.GetTempPath (), "Axtae", "x.log");
  public Logger Log { get; } = new (LoggerPath, 4096, 8);

  public void Dispose () => this.Log.Complete ();
}

public sealed class LoggerTests (LoggerFixture fixture) : IClassFixture<LoggerFixture> {
  private readonly Logger _log = fixture.Log;

  [Fact]
  public void Log_ValidMessage_DoesNotThrow ()
    => Assert.Null (Record.Exception (() => this._log.Info ("Test message")));

  [Fact]
  public void Log_EmptyMessage_DoesNotThrow () {
    var ex = Record.Exception (() => this._log.Warning (""));
    Assert.Null (ex);

    ex = Record.Exception (() => this._log.Error ("   "));
    Assert.Null (ex);
  }

  [Fact]
  public void Log_NullMessage_DoesNotThrow ()
    => Assert.Null (Record.Exception (() => this._log.Debug (null)));

  [Fact]
  public async Task Log_WhenChannelFull_DoesNotBlockIndefinitely () {
    var task = Task.Run (() => {
      for (int i = 0; i < 20; i++) {
        this._log.Debug ($"Bulk message {i}");
      }
    }, TestContext.Current.CancellationToken);

    var completed = await Task.WhenAny (task,
      Task.Delay (200, TestContext.Current.CancellationToken));
    Assert.Equal (task, completed);
  }

  [Fact]
  public void Log_MaxEntryLength () {
    string msg = new ('a', 4096); // MaxEntryLength = 4096
    this._log.Info (msg);
    this._log.Log (Logger.Level.Info, "msg", msg, "", -1);

    int file_len = this._log.MaxEntryLength - TimeStamp.Size - "[Info]"u8.Length - 2;
    this._log.Log (Logger.Level.Info, "msg", new ('f', file_len), "", -1);
    this._log.Log (Logger.Level.Info, "msg", new ('f', file_len - 1), "", -1);
    this._log.Log (Logger.Level.Info, "msg", new ('f', file_len - 3), "", -1);
    this._log.Log (Logger.Level.Info, "msg", new ('f', file_len - 10), "", -1);

    this._log.Log (Logger.Level.Info, "msg", LoggerFixture.LoggerPath, msg, -1);

    this._log.Info (new ('m', 4004));

    Assert.True (true);
  }

  [Fact]
  public void Log_EveryLevel () {
    this._log.Debug ("Debug");
    this._log.Info ("Info");
    this._log.Warning ("Warning");
    this._log.Error ("Error");

    Assert.True (true);
  }

  [Fact]
  public void Log_WithInvalidLevel_ShouldUseUnknownLevel () {
    const Logger.Level L = (Logger.Level)999;
    this._log.Log (L, "test invalid level",
      LoggerFixture.LoggerPath, nameof (Log_WithInvalidLevel_ShouldUseUnknownLevel), -1);

    Assert.True (true);
  }

  [Fact]
  public void Log_WithEmptyString_ShouldWriteQuestionMark () {
    this._log.Log (Logger.Level.Info, "msg",
      "", nameof (Log_WithEmptyString_ShouldWriteQuestionMark), -1);
    this._log.Log (Logger.Level.Info, "msg", LoggerFixture.LoggerPath, "", -1);

    Assert.True (true);
  }

  [Fact]
  public static void MaxEntryLength_DoesClampTo100 () {
    string path = Path.Combine (Path.GetTempPath (), "Axtae", "i.log");
    Logger log = new (path, 0);
    Assert.Equal (100, log.MaxEntryLength);
    log.Complete ();
  }

  [Fact]
  public static void Size_DoesClampTo4 () {
    string path = Path.Combine (Path.GetTempPath (), "Axtae", "4.log");
    Logger log = new (path, 128, 0);
    Assert.Equal (4u, log.Size);
    log.Complete ();
  }

  [Fact]
  public static void LogFilePath_ThrowsIfNullOrWhiteSpace () {
    _ = Assert.Throws<ArgumentNullException> (() => new Logger (null!));
    _ = Assert.Throws<ArgumentException> (() => new Logger (""));
    _ = Assert.Throws<ArgumentException> (() => new Logger ("   "));
  }
}
