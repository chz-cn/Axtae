// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using Microsoft.CodeAnalysis;

namespace Axtae.Generator;

/// <summary>
/// A source generator that emits logger extension methods for the Axtae
/// logging API.
/// </summary>
[Generator]
public sealed class Logger : IIncrementalGenerator {
  /// <summary>
  /// The generated source for the logger extension methods that are exposed to
  /// consumers of the Axtae library.
  /// </summary>
  public const string LoggerExtensionsSource = /* lang=C# */"""

using System.Runtime.CompilerServices;

namespace Axtae;

internal static class LoggerExtensions {
  extension (global::Axtae.Logger log) {
    /// <summary>
    /// Logs a debug message (only compiled in DEBUG builds).
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="file">
    /// The source file path, automatically filled by the compiler.
    /// </param>
    /// <param name="member">
    /// The calling member name, automatically filled by the compiler.
    /// </param>
    /// <param name="line">
    /// The line number, automatically filled by the compiler.
    /// </param>
    /// <remarks>
    /// This method is a no-op in RELEASE builds due to the
    /// <see cref="System.Diagnostics.ConditionalAttribute"/>.
    /// </remarks>
    /// <exception cref="System.ArgumentException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/> is invalid.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>
    /// has been closed.
    /// </exception>
    /// <exception cref="System.UnauthorizedAccessException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>
    /// was not opened for writing.
    /// </exception>
    /// <exception cref="System.IO.IOException">
    /// An I/O error occurred.
    /// </exception>
    [System.Diagnostics.Conditional ("DEBUG")]
    public void Debug (string msg,
      [CallerFilePath] string file = "",
      [CallerMemberName] string member = "",
      [CallerLineNumber] int line = 0
    ) => log.Log (new (
      TimeStamp.UtcNow,
      msg,
      file,
      member,
      line,
      global::Axtae.Logger.Level.Debug
    ));

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="file">
    /// The source file path, automatically filled by the compiler.
    /// In DEBUG builds it is the actual path; otherwise empty.
    /// </param>
    /// <param name="member">
    /// The calling member name, automatically filled by the compiler.
    /// In DEBUG builds it is the actual member; otherwise empty.
    /// </param>
    /// <param name="line">
    /// The line number, automatically filled by the compiler.
    /// In DEBUG builds it is the actual line; otherwise -1.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/> is invalid.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>
    /// has been closed.
    /// </exception>
    /// <exception cref="System.UnauthorizedAccessException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>
    /// was not opened for writing.
    /// </exception>
    /// <exception cref="System.IO.IOException">
    /// An I/O error occurred.
    /// </exception>
    public void Info (string msg,
#if DEBUG
      [CallerFilePath] string file = "",
      [CallerMemberName] string member = "",
      [CallerLineNumber] int line = 0
#else
      string file = "",
      string member = "",
      int line = -1
#endif
    ) => log.Log (new (
      TimeStamp.UtcNow,
      msg,
      file,
      member,
      line,
      global::Axtae.Logger.Level.Info
    ));

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="file">
    /// The source file path, automatically filled by the compiler.
    /// In DEBUG builds it is the actual path; otherwise empty.
    /// </param>
    /// <param name="member">
    /// The calling member name, automatically filled by the compiler.
    /// In DEBUG builds it is the actual member; otherwise empty.
    /// </param>
    /// <param name="line">
    /// The line number, automatically filled by the compiler.
    /// In DEBUG builds it is the actual line; otherwise -1.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/> is invalid.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>
    /// has been closed.
    /// </exception>
    /// <exception cref="System.UnauthorizedAccessException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>
    /// was not opened for writing.
    /// </exception>
    /// <exception cref="System.IO.IOException">
    /// An I/O error occurred.
    /// </exception>
    public void Warning (string msg,
#if DEBUG
      [CallerFilePath] string file = "",
      [CallerMemberName] string member = "",
      [CallerLineNumber] int line = 0
#else
      string file = "",
      string member = "",
      int line = -1
#endif
    ) => log.Log (new (
      TimeStamp.UtcNow,
      msg,
      file,
      member,
      line,
      global::Axtae.Logger.Level.Warning
    ));

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="file">
    /// The source file path, automatically filled by the compiler.
    /// In DEBUG builds it is the actual path; otherwise "?".
    /// </param>
    /// <param name="line">
    /// The line number, automatically filled by the compiler.
    /// In DEBUG builds it is the actual line; otherwise -1.
    /// </param>
    /// <param name="member">
    /// The calling member name, automatically filled by the compiler.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/> is invalid.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>
    /// has been closed.
    /// </exception>
    /// <exception cref="System.UnauthorizedAccessException">
    /// <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>
    /// was not opened for writing.
    /// </exception>
    /// <exception cref="System.IO.IOException">
    /// An I/O error occurred.
    /// </exception>
    public void Error (string msg,
#if DEBUG
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0,
#else
      string file = "?",
      int line = -1,
#endif
      [CallerMemberName] string member = ""
    ) => log.Log (new (
      TimeStamp.UtcNow,
      msg,
      file,
      member,
      line,
      global::Axtae.Logger.Level.Error
    ));
  }
}

""";

  /// <summary>
  /// Registers the source generation callback that adds the logger extension
  /// methods.
  /// </summary>
  /// <param name="context">The generator initialization context.</param>
  public void Initialize (IncrementalGeneratorInitializationContext context)
    => context.RegisterPostInitializationOutput (
      ctx => ctx.AddSource ("LoggerExtensions.g.cs", LoggerExtensionsSource)
    );
}
