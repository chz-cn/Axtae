// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;

using Axtae;

using Xunit;

namespace Test;

public static class EntryTests {
  public static readonly DateTime T0
    = new (2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
  public static readonly DateTime T1 = T0.AddTicks (1);

  public static Logger.Entry Make (
    DateTime timestamp = default,
    string msg = "msg",
    string file = "file",
    string member = "member",
    int line = 1,
    Logger.Level level = Logger.Level.Info
  ) => new (
    timestamp == default ? T0 : timestamp,
    msg,
    file,
    member,
    line,
    level
  );

  [Fact]
  public static void Equals_SameFields_ReturnsTrue () {
    var a = Make ();
    var b = Make ();
    Assert.True (a.Equals (b));
    Assert.True (a == b);
    Assert.False (a != b);
  }

  [Fact]
  public static void Equals_DiffersInTimestamp_ReturnsFalse () {
    Assert.False (Make ().Equals (Make (timestamp: T1)));
    Assert.True (Make () != Make (timestamp: T1));
  }

  [Fact]
  public static void Equals_DiffersInMsg_ReturnsFalse () {
    Assert.False (Make ().Equals (Make (msg: "other")));
    Assert.True (Make () != Make (msg: "other"));
  }

  [Fact]
  public static void Equals_DiffersInFile_ReturnsFalse () {
    Assert.False (Make ().Equals (Make (file: "other")));
    Assert.True (Make () != Make (file: "other"));
  }

  [Fact]
  public static void Equals_DiffersInMember_ReturnsFalse () {
    Assert.False (Make ().Equals (Make (member: "other")));
    Assert.True (Make () != Make (member: "other"));
  }

  [Fact]
  public static void Equals_DiffersInLine_ReturnsFalse () {
    Assert.False (Make ().Equals (Make (line: 2)));
    Assert.True (Make () != Make (line: 2));
  }

  [Fact]
  public static void Equals_DiffersInLevel_ReturnsFalse () {
    Assert.False (Make ().Equals (Make (level: Logger.Level.Error)));
    Assert.True (Make () != Make (level: Logger.Level.Error));
  }

  [Fact]
  public static void Equals_Object_DispatchesToTypedEquals () {
    object boxed = Make ();
    Assert.True (Make ().Equals (boxed));
    Assert.False (Make ().Equals (null));
    Assert.False (Make ().Equals ("not an entry"));
  }

  [Fact]
  public static void GetHashCode_EqualEntries_ReturnsSameValue ()
    => Assert.Equal (Make ().GetHashCode (), Make ().GetHashCode ());

  [Fact]
  public static void GetHashCode_DifferentEntries_UsuallyDiffer ()
    // Not a contract guarantee, but a sanity check that the hash
    // actually mixes all fields.
    => Assert.NotEqual (
      Make ().GetHashCode (),
      Make (msg: "other").GetHashCode ()
    );
}
