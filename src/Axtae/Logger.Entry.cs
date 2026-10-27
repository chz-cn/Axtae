// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;

namespace Axtae;

public partial class Logger {
  /// <summary>
  /// Defines the severity levels for log entries.
  /// </summary>
  public enum Level {
    /// <summary>Debugging information, used only in DEBUG builds.</summary>
    Debug = 1,
    /// <summary>Informational messages.</summary>
    Info = 2,
    /// <summary>Warning conditions that are not errors.</summary>
    Warning = 3,
    /// <summary>Error conditions that require attention.</summary>
    Error = 4
  }

  /// <summary>
  /// Represents a single log entry with all contextual information.
  /// </summary>
  /// <param name="timestamp">
  /// The timestamp when the log entry was created.
  /// </param>
  /// <param name="msg">The log message.</param>
  /// <param name="file">
  /// The source file path from which the log was called.
  /// </param>
  /// <param name="member">
  /// The member name (method/property) from which the log was called.
  /// </param>
  /// <param name="line">The source line number.</param>
  /// <param name="level">The severity level of the log entry.</param>
  /// <remarks>
  /// This struct is immutable after construction. It is passed by reference
  /// internally to avoid copying.
  /// </remarks>
  public readonly struct Entry (
    DateTime timestamp,
    string msg,
    string file,
    string member,
    int line,
    Level level
  ) : IEquatable<Entry> {
    /// <summary>The timestamp when the log entry was created.</summary>
    public readonly DateTime Timestamp = timestamp;

    /// <summary>The log message.</summary>
    public readonly string Msg = msg;

    /// <summary>The source file path from which the log was called.</summary>
    public readonly string File = file;

    /// <summary>
    /// The member name (method/property) from which the log was called.
    /// </summary>
    public readonly string Member = member;

    /// <summary>The source line number.</summary>
    public readonly int Line = line;

    /// <summary>The severity level of the log entry.</summary>
    public readonly Level Level = level;

    /// <inheritdoc />
    public bool Equals (Entry other)
      => this.Timestamp == other.Timestamp
      && this.Msg == other.Msg
      && this.File == other.File
      && this.Member == other.Member
      && this.Line == other.Line
      && this.Level == other.Level;

    /// <inheritdoc />
    public override bool Equals (object? obj)
      => obj is Entry other && this.Equals (other);

    /// <inheritdoc />
    public override int GetHashCode () => HashCode.Combine (
      this.Timestamp,
      this.Msg,
      this.File,
      this.Member,
      this.Line,
      this.Level
    );

    /// <summary>Determines whether two entries are equal.</summary>
    /// <param name="left">The first entry to compare.</param>
    /// <param name="right">The second entry to compare.</param>
    public static bool operator == (Entry left, Entry right)
      => left.Equals (right);

    /// <summary>Determines whether two entries are not equal.</summary>
    /// <param name="left">The first entry to compare.</param>
    /// <param name="right">The second entry to compare.</param>
    public static bool operator != (Entry left, Entry right)
      => !(left == right);
  }
}
