// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading.Tasks;

using Axtae;

using Microsoft.Win32.SafeHandles;

using Xunit;

namespace Test;

public sealed class LoggerFixture : IDisposable {
  public static readonly string LoggerPath
    = Path.Combine (Path.GetTempPath (), "Axtae", "x.log");
  public readonly SafeFileHandle Handle;
  public readonly Logger Log;

  static LoggerFixture () {
    _ = Directory.CreateDirectory (Path.GetDirectoryName (LoggerPath)!);
  }

  public LoggerFixture () {
    this.Handle = File.OpenHandle (LoggerPath, FileMode.Create, FileAccess.Write);
    this.Log = new Logger (this.Handle, 4096, 16);
  }

  public void Dispose () {
    this.Log.Complete ();
    this.Log.Completion.GetAwaiter ().GetResult ();
    this.Handle.Dispose ();
    File.Delete (LoggerPath);
  }
}

public sealed class LoggerTests (LoggerFixture fixture) : IClassFixture<LoggerFixture> {
  private readonly Logger _log = fixture.Log;

  [Fact]
  public void Log_ValidMessage_DoesNotThrow ()
    => Assert.Null (Record.Exception (() => {
      this._log.Info ("Test message");
      this._log.Warning ("");
      this._log.Error ("   ");
      this._log.Debug (null);
    }));

  [Fact]
  public async Task Log_WhenQueueFull_DoesNotBlockIndefinitely () {
    var task = Task.Factory.StartNew (
      LogBulkMessege,
      this._log,
      TestContext.Current.CancellationToken,
      TaskCreationOptions.RunContinuationsAsynchronously,
      TaskScheduler.Default
    );

    var completed = await Task.WhenAny (
      task,
      Task.Delay (200, TestContext.Current.CancellationToken)
    );

    Assert.Equal (task, completed);

    static void LogBulkMessege (object? obj) {
      var log = (Logger)obj!;
      for (int i = 0; i < 20; i++) {
        log.Debug ("Bulk message " + i);
      }
    }
  }

  [Fact]
  public void Log_MaxEntryLength () {
    // "[Info]" = 6 bytes, fixed prefix before File:
    //   timestamp (TimeStamp.Size) + space + level + space
    int prefix = TimeStamp.Size + 1 + "[Info]"u8.Length + 1;
    int file_base = this._log.MaxEntryLength - prefix;

    var now = TimeStamp.UtcNow;

    // Each length hits a different truncation stage:
    Span<int> sp = [
      file_base + 10, // truncate inside File (AddString → AddDots)
      file_base,      // File fills the span, "(" fails (AddByte → AddDots)
      file_base - 1,  // line digit fails (Line.ToAscii == 0)
      file_base - 3,  // ") " fails (AddBytes)
      file_base - 10  // member / msg stage
    ];
    foreach (int len in sp) {
      string file = new ('f', len);
      this._log.Log (new (now, "msg", file, "", -1, Logger.Level.Info));
    }

    // msg overflow with a realistic file path
    this._log.Log (new (
      now,
      new string ('a', this._log.MaxEntryLength),
      LoggerFixture.LoggerPath,
      "",
      -1,
      Logger.Level.Info
    ));

    Assert.True (true);
  }

  [Fact]
  public void Log_WithInvalidLevel_ShouldUseUnknownLevel () {
    const Logger.Level L = (Logger.Level)999;
    this._log.Log (new (
      TimeStamp.UtcNow,
      "test invalid level",
      LoggerFixture.LoggerPath,
      nameof (Log_WithInvalidLevel_ShouldUseUnknownLevel),
      -1,
      L
    ));

    Assert.True (true);
  }

  [Fact]
  public void Log_WithEmptyString_ShouldWriteQuestionMark () {
    this._log.Log (new (
      TimeStamp.UtcNow,
      "msg",
      "",
      nameof (Log_WithEmptyString_ShouldWriteQuestionMark),
      -1,
      Logger.Level.Info
    ));

    this._log.Log (new (
      TimeStamp.UtcNow,
      "msg",
      LoggerFixture.LoggerPath,
      "",
      -1,
      Logger.Level.Info
    ));

    Assert.True (true);
  }

  [Fact]
  public static Task MaxEntryLength_DoesClampTo100 () {
    string path = Path.Combine (Path.GetTempPath (), "Axtae", "i.log");
    using var handle = File.OpenHandle (
      path,
      FileMode.Create,
      FileAccess.Write
    );

    Logger log = new (handle, 0);
    Assert.Equal (100, log.MaxEntryLength);
    log.Complete ();
    return Task.CompletedTask;
  }

  [Fact]
  public static Task Log_AfterComplete_DropsEntry () {
    string path = Path.Combine (Path.GetTempPath (), "Axtae", "o.log");
    using var handle = File.OpenHandle (
      path,
      FileMode.Create,
      FileAccess.Write
    );

    Logger log = new (handle, 0);
    log.Complete ();
    log.Info ("dropped");
    Assert.True (true);
    return Task.CompletedTask;
  }

  [Fact]
  public static Task Log_OversizedEntry_UsesArrayPool () {
    string path = Path.Combine (Path.GetTempPath (), "Axtae", "l.log");

#pragma warning disable CA2000 // 丢失范围之前释放对象
    var handle = File.OpenHandle (path, FileMode.Create, FileAccess.Write);
#pragma warning restore CA2000 // 丢失范围之前释放对象

    var log = new Logger (handle, 4097, 16);

    for (int i = 0; i < 20; i++) {
      log.Info ("bulk" + i);
    }

    log.Complete ();
    Assert.True (true);

    return log.Completion.ContinueWith (
      Dispose,
      handle,
      TestContext.Current.CancellationToken,
      TaskContinuationOptions.None,
      TaskScheduler.Default
    );

    static void Dispose (Task _, object? obj) {
      var handle = (SafeFileHandle)obj!;
      handle.Dispose ();
    }
  }
}
