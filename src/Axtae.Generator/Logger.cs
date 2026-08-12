
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
  public const string LoggerExtensionsSource = """

using System.Runtime.CompilerServices;

namespace Axtae;

public static class LoggerExtensions {
  extension(global::Axtae.Logger log) {
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
    [System.Diagnostics.Conditional("DEBUG")]
    public void Debug(string msg,
      [CallerFilePath] string file = "",
      [CallerMemberName] string member = "",
      [CallerLineNumber] int line = 0
    ) => log.Log(Logger.Level.Debug, msg, file, member, line);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="file">
    /// The source file path, automatically filled by the compiler.
    /// In DEBUG builds it is the actual path; otherwise a placeholder.
    /// </param>
    /// <param name="member">
    /// The calling member name, automatically filled by the compiler.
    /// In DEBUG builds it is the actual path; otherwise a placeholder.
    /// </param>
    /// <param name="line">
    /// The line number, automatically filled by the compiler.
    /// In DEBUG builds it is the actual line; otherwise -1.
    /// </param>
    public void Info(string msg,
#if DEBUG
      [CallerFilePath] string file = "",
      [CallerMemberName] string member = "",
      [CallerLineNumber] int line = 0
#else
      string file = "",
      string member = "",
      int line = -1
#endif
    ) => log.Log(Logger.Level.Info, msg, file, member, line);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="file">
    /// The source file path, automatically filled by the compiler.
    /// In DEBUG builds it is the actual path; otherwise a placeholder.
    /// </param>
    /// <param name="member">
    /// The calling member name, automatically filled by the compiler.
    /// In DEBUG builds it is the actual path; otherwise a placeholder.
    /// </param>
    /// <param name="line">
    /// The line number, automatically filled by the compiler.
    /// In DEBUG builds it is the actual line; otherwise -1.
    /// </param>
    public void Warning(string msg,
#if DEBUG
      [CallerFilePath] string file = "",
      [CallerMemberName] string member = "",
      [CallerLineNumber] int line = 0
#else
      string file = "",
      string member = "",
      int line = -1
#endif
    ) => log.Log(Logger.Level.Warning, msg, file, member, line);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    /// <param name="member">
    /// The calling member name, automatically filled by the compiler.
    /// It will not trim without DEBUG.
    /// </param>
    /// <param name="file">
    /// The source file path, automatically filled by the compiler.
    /// In DEBUG builds it is the actual path; otherwise a placeholder.
    /// </param>
    /// <param name="line">
    /// The line number, automatically filled by the compiler.
    /// In DEBUG builds it is the actual line; otherwise -1.
    /// </param>
    public void Error(string msg,
#if DEBUG
      [CallerFilePath] string file = "",
      [CallerLineNumber] int line = 0,
#else
      string file = "?",
      int line = -1,
#endif
      [CallerMemberName] string member = ""
    ) => log.Log(Logger.Level.Error, msg, file, member, line);
  }
}

""";

  /// <summary>
  /// Registers the source generation callback that adds the logger extension
  /// methods.
  /// </summary>
  /// <param name="context">The generator initialization context.</param>
  public void Initialize(IncrementalGeneratorInitializationContext context)
    => context.RegisterPostInitializationOutput(
      ctx => ctx.AddSource("LoggerExtensions.g.cs", LoggerExtensionsSource));
}
