
using System;
using System.Runtime.CompilerServices;

namespace Axtae.Encode;

/// <summary>
/// Provides constant values for ASCII control characters and printable
/// characters, along with high‑performance helper methods for converting
/// integers to ASCII strings.
/// </summary>
/// <remarks>
/// <para>
/// All constants are of type <see cref="byte"/> and correspond to the ASCII
/// table.
/// Control characters (0–31) are named with their standard abbreviations
/// (e.g., <see cref="NUL"/>),
/// while printable characters (32–126) are named for the glyph they represent.
/// </para>
/// <para>
/// The class also provides allocation‑free conversion of <see cref="int"/>
/// and <see cref="uint"/> to ASCII byte spans via the extension methods,
/// which use a precomputed lookup table for two‑digit groups.
/// </para>
/// <para>
/// All methods are thread‑safe and inline‑optimized for high performance.
/// </para>
/// </remarks>
public static class Ascii {
  // Control characters (0–31)
  /// <summary>Null (NUL), ASCII 0.</summary>
  public const byte NUL = 0;
  /// <summary>Start of Heading (SOH), ASCII 1.</summary>
  public const byte SOH = 1;
  /// <summary>Start of Text (STX), ASCII 2.</summary>
  public const byte STX = 2;
  /// <summary>End of Text (ETX), ASCII 3.</summary>
  public const byte ETX = 3;
  /// <summary>End of Transmission (EOT), ASCII 4.</summary>
  public const byte EOT = 4;
  /// <summary>Enquiry (ENQ), ASCII 5.</summary>
  public const byte ENQ = 5;
  /// <summary>Acknowledge (ACK), ASCII 6.</summary>
  public const byte ACK = 6;
  /// <summary>Bell (BEL), ASCII 7.</summary>
  public const byte BEL = 7;
  /// <summary>Backspace (BS), ASCII 8.</summary>
  public const byte Backspace = 8;
  /// <summary>Horizontal Tab (HT), ASCII 9.</summary>
  public const byte HT = 9;
  /// <summary>Line Feed (LF), ASCII 10.</summary>
  public const byte LF = 10;
  /// <summary>Vertical Tab (VT), ASCII 11.</summary>
  public const byte VT = 11;
  /// <summary>Form Feed (FF), ASCII 12.</summary>
  public const byte FF = 12;
  /// <summary>Carriage Return (CR), ASCII 13.</summary>
  public const byte CR = 13;
  /// <summary>Shift Out (SO), ASCII 14.</summary>
  public const byte SO = 14;
  /// <summary>Shift In (SI), ASCII 15.</summary>
  public const byte SI = 15;
  /// <summary>Data Link Escape (DLE), ASCII 16.</summary>
  public const byte DLE = 16;
  /// <summary>Device Control 1 (DC1), ASCII 17.</summary>
  public const byte DC1 = 17;
  /// <summary>Device Control 2 (DC2), ASCII 18.</summary>
  public const byte DC2 = 18;
  /// <summary>Device Control 3 (DC3), ASCII 19.</summary>
  public const byte DC3 = 19;
  /// <summary>Device Control 4 (DC4), ASCII 20.</summary>
  public const byte DC4 = 20;
  /// <summary>Negative Acknowledge (NAK), ASCII 21.</summary>
  public const byte NAK = 21;
  /// <summary>Synchronous Idle (SYN), ASCII 22.</summary>
  public const byte SYN = 22;
  /// <summary>End of Transmission Block (ETB), ASCII 23.</summary>
  public const byte ETB = 23;
  /// <summary>Cancel (CAN), ASCII 24.</summary>
  public const byte CAN = 24;
  /// <summary>End of Medium (EM), ASCII 25.</summary>
  public const byte EM = 25;
  /// <summary>Substitute (SUB), ASCII 26.</summary>
  public const byte SUB = 26;
  /// <summary>Escape (ESC), ASCII 27.</summary>
  public const byte ESC = 27;
  /// <summary>File Separator (FS), ASCII 28.</summary>
  public const byte FS = 28;
  /// <summary>Group Separator (GS), ASCII 29.</summary>
  public const byte GS = 29;
  /// <summary>Record Separator (RS), ASCII 30.</summary>
  public const byte RS = 30;
  /// <summary>Unit Separator (US), ASCII 31.</summary>
  public const byte US = 31;

  // Printable characters (32–126)
  /// <summary>Space, ASCII 32.</summary>
  public const byte Space = 32;
  /// <summary>Exclamation mark '!', ASCII 33.</summary>
  public const byte ExclamationMark = (byte)'!';
  /// <summary>Quotation mark '"', ASCII 34.</summary>
  public const byte QuotationMark = (byte)'"';
  /// <summary>Number sign '#', ASCII 35.</summary>
  public const byte NumberSign = (byte)'#';
  /// <summary>Dollar sign '$', ASCII 36.</summary>
  public const byte DollarSign = (byte)'$';
  /// <summary>Percent sign '%', ASCII 37.</summary>
  public const byte PercentSign = (byte)'%';
  /// <summary>Ampersand '&amp;', ASCII 38.</summary>
  public const byte Ampersand = (byte)'&';
  /// <summary>Apostrophe '\'', ASCII 39.</summary>
  public const byte Apostrophe = (byte)'\'';
  /// <summary>Open parenthesis '(', ASCII 40.</summary>
  public const byte OpenParenthesis = (byte)'(';
  /// <summary>Close parenthesis ')', ASCII 41.</summary>
  public const byte CloseParenthesis = (byte)')';
  /// <summary>Asterisk '*', ASCII 42.</summary>
  public const byte Asterisk = (byte)'*';
  /// <summary>Plus sign '+', ASCII 43.</summary>
  public const byte PlusSign = (byte)'+';
  /// <summary>Comma ',', ASCII 44.</summary>
  public const byte Comma = (byte)',';
  /// <summary>Hyphen‑minus '-', ASCII 45.</summary>
  public const byte HyphenMinus = (byte)'-';
  /// <summary>Period '.', ASCII 46.</summary>
  public const byte Period = (byte)'.';
  /// <summary>Slash '/', ASCII 47.</summary>
  public const byte Slash = (byte)'/';

  // Numeric digits
  /// <summary>Digit '0', ASCII 48.</summary>
  public const byte Zero = (byte)'0';
  /// <summary>Digit '1', ASCII 49.</summary>
  public const byte One = (byte)'1';
  /// <summary>Digit '2', ASCII 50.</summary>
  public const byte Two = (byte)'2';
  /// <summary>Digit '3', ASCII 51.</summary>
  public const byte Three = (byte)'3';
  /// <summary>Digit '4', ASCII 52.</summary>
  public const byte Four = (byte)'4';
  /// <summary>Digit '5', ASCII 53.</summary>
  public const byte Five = (byte)'5';
  /// <summary>Digit '6', ASCII 54.</summary>
  public const byte Six = (byte)'6';
  /// <summary>Digit '7', ASCII 55.</summary>
  public const byte Seven = (byte)'7';
  /// <summary>Digit '8', ASCII 56.</summary>
  public const byte Eight = (byte)'8';
  /// <summary>Digit '9', ASCII 57.</summary>
  public const byte Nine = (byte)'9';

  // Additional punctuation
  /// <summary>Colon ':', ASCII 58.</summary>
  public const byte Colon = (byte)':';
  /// <summary>Semicolon ';', ASCII 59.</summary>
  public const byte Semicolon = (byte)';';
  /// <summary>Less‑than sign '&lt;', ASCII 60.</summary>
  public const byte LessThan = (byte)'<';
  /// <summary>Equal sign '=', ASCII 61.</summary>
  public const byte EqualSign = (byte)'=';
  /// <summary>Greater‑than sign '&gt;', ASCII 62.</summary>
  public const byte GreaterThan = (byte)'>';
  /// <summary>Question mark '?', ASCII 63.</summary>
  public const byte QuestionMark = (byte)'?';
  /// <summary>At sign '@', ASCII 64.</summary>
  public const byte AtSign = (byte)'@';

  // Uppercase letters
  /// <summary>'A', ASCII 65.</summary>
  public const byte A = (byte)'A';
  /// <summary>'B', ASCII 66.</summary>
  public const byte B = (byte)'B';
  /// <summary>'C', ASCII 67.</summary>
  public const byte C = (byte)'C';
  /// <summary>'D', ASCII 68.</summary>
  public const byte D = (byte)'D';
  /// <summary>'E', ASCII 69.</summary>
  public const byte E = (byte)'E';
  /// <summary>'F', ASCII 70.</summary>
  public const byte F = (byte)'F';
  /// <summary>'G', ASCII 71.</summary>
  public const byte G = (byte)'G';
  /// <summary>'H', ASCII 72.</summary>
  public const byte H = (byte)'H';
  /// <summary>'I', ASCII 73.</summary>
  public const byte I = (byte)'I';
  /// <summary>'J', ASCII 74.</summary>
  public const byte J = (byte)'J';
  /// <summary>'K', ASCII 75.</summary>
  public const byte K = (byte)'K';
  /// <summary>'L', ASCII 76.</summary>
  public const byte L = (byte)'L';
  /// <summary>'M', ASCII 77.</summary>
  public const byte M = (byte)'M';
  /// <summary>'N', ASCII 78.</summary>
  public const byte N = (byte)'N';
  /// <summary>'O', ASCII 79.</summary>
  public const byte O = (byte)'O';
  /// <summary>'P', ASCII 80.</summary>
  public const byte P = (byte)'P';
  /// <summary>'Q', ASCII 81.</summary>
  public const byte Q = (byte)'Q';
  /// <summary>'R', ASCII 82.</summary>
  public const byte R = (byte)'R';
  /// <summary>'S', ASCII 83.</summary>
  public const byte S = (byte)'S';
  /// <summary>'T', ASCII 84.</summary>
  public const byte T = (byte)'T';
  /// <summary>'U', ASCII 85.</summary>
  public const byte U = (byte)'U';
  /// <summary>'V', ASCII 86.</summary>
  public const byte V = (byte)'V';
  /// <summary>'W', ASCII 87.</summary>
  public const byte W = (byte)'W';
  /// <summary>'X', ASCII 88.</summary>
  public const byte X = (byte)'X';
  /// <summary>'Y', ASCII 89.</summary>
  public const byte Y = (byte)'Y';
  /// <summary>'Z', ASCII 90.</summary>
  public const byte Z = (byte)'Z';

  /// <summary>Open bracket '[', ASCII 91.</summary>
  public const byte OpenBracket = (byte)'[';
  /// <summary>Backslash '\', ASCII 92.</summary>
  public const byte Backslash = (byte)'\\';
  /// <summary>Close bracket ']', ASCII 93.</summary>
  public const byte CloseBracket = (byte)']';
  /// <summary>Caret '^', ASCII 94.</summary>
  public const byte Caret = (byte)'^';
  /// <summary>Underscore '_', ASCII 95.</summary>
  public const byte Underscore = (byte)'_';
  /// <summary>Backtick '`', ASCII 96.</summary>
  public const byte Backtick = (byte)'`';

#pragma warning disable IDE1006 // 命名样式
  // Lowercase letters (named with lowercase to match their glyph)
  /// <summary>'a', ASCII 97.</summary>
  public const byte a = (byte)'a';
  /// <summary>'b', ASCII 98.</summary>
  public const byte b = (byte)'b';
  /// <summary>'c', ASCII 99.</summary>
  public const byte c = (byte)'c';
  /// <summary>'d', ASCII 100.</summary>
  public const byte d = (byte)'d';
  /// <summary>'e', ASCII 101.</summary>
  public const byte e = (byte)'e';
  /// <summary>'f', ASCII 102.</summary>
  public const byte f = (byte)'f';
  /// <summary>'g', ASCII 103.</summary>
  public const byte g = (byte)'g';
  /// <summary>'h', ASCII 104.</summary>
  public const byte h = (byte)'h';
  /// <summary>'i', ASCII 105.</summary>
  public const byte i = (byte)'i';
  /// <summary>'j', ASCII 106.</summary>
  public const byte j = (byte)'j';
  /// <summary>'k', ASCII 107.</summary>
  public const byte k = (byte)'k';
  /// <summary>'l', ASCII 108.</summary>
  public const byte l = (byte)'l';
  /// <summary>'m', ASCII 109.</summary>
  public const byte m = (byte)'m';
  /// <summary>'n', ASCII 110.</summary>
  public const byte n = (byte)'n';
  /// <summary>'o', ASCII 111.</summary>
  public const byte o = (byte)'o';
  /// <summary>'p', ASCII 112.</summary>
  public const byte p = (byte)'p';
  /// <summary>'q', ASCII 113.</summary>
  public const byte q = (byte)'q';
  /// <summary>'r', ASCII 114.</summary>
  public const byte r = (byte)'r';
  /// <summary>'s', ASCII 115.</summary>
  public const byte s = (byte)'s';
  /// <summary>'t', ASCII 116.</summary>
  public const byte t = (byte)'t';
  /// <summary>'u', ASCII 117.</summary>
  public const byte u = (byte)'u';
  /// <summary>'v', ASCII 118.</summary>
  public const byte v = (byte)'v';
  /// <summary>'w', ASCII 119.</summary>
  public const byte w = (byte)'w';
  /// <summary>'x', ASCII 120.</summary>
  public const byte x = (byte)'x';
  /// <summary>'y', ASCII 121.</summary>
  public const byte y = (byte)'y';
  /// <summary>'z', ASCII 122.</summary>
  public const byte z = (byte)'z';
#pragma warning restore IDE1006 // 命名样式

  /// <summary>Open brace '{', ASCII 123.</summary>
  public const byte OpenBrace = (byte)'{';
  /// <summary>Pipe '|', ASCII 124.</summary>
  public const byte Pipe = (byte)'|';
  /// <summary>Close brace '}', ASCII 125.</summary>
  public const byte CloseBrace = (byte)'}';
  /// <summary>Tilde '~', ASCII 126.</summary>
  public const byte Tilde = (byte)'~';
  /// <summary>Delete (DEL), ASCII 127.</summary>
  public const byte DEL = 127;

  /// <summary>
  /// Precomputed lookup table for two‑digit decimal numbers (00–99) as ASCII
  /// bytes.
  /// </summary>
  public static ReadOnlySpan<byte> TwoDigit =>
    "00010203040506070809"u8 +
    "10111213141516171819"u8 +
    "20212223242526272829"u8 +
    "30313233343536373839"u8 +
    "40414243444546474849"u8 +
    "50515253545556575859"u8 +
    "60616263646566676869"u8 +
    "70717273747576777879"u8 +
    "80818283848586878889"u8 +
    "90919293949596979899"u8;

  /// <summary>
  /// Computes the number of decimal digits in a <see cref="uint"/> value.
  /// </summary>
  /// <param name="value">
  /// The unsigned integer whose digit count is needed.
  /// </param>
  /// <returns>
  /// The number of digits (1–10) required to represent
  /// <paramref name="value"/>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// The implementation uses a lookup table and a fast integer division‑like
  /// trick that avoids branching. The table is indexed by <c>Log2(value)</c>
  /// to obtain a threshold; the digit count is derived from
  /// <c>(value + threshold) &gt;&gt; 32</c>.
  /// </para>
  /// <para>
  /// This method is inlined and should be used only for positive values;
  /// for zero, it returns 1 (since zero has one digit).
  /// </para>
  /// </remarks>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int CountDigits(uint value) {
    ReadOnlySpan<long> table = [
      4294967296,
      8589934582,  8589934582,  8589934582,
      12884901788, 12884901788, 12884901788,
      17179868184, 17179868184, 17179868184,
      21474826480, 21474826480, 21474826480, 21474826480,
      25769703776, 25769703776, 25769703776,
      30063771072, 30063771072, 30063771072,
      34349738368, 34349738368, 34349738368, 34349738368,
      38554705664, 38554705664, 38554705664,
      41949672960, 41949672960, 41949672960,
      42949672960, 42949672960
    ];

    long tableValue = table[(int)uint.Log2(value)];
    return (int)((value + tableValue) >> 32);
  }

  extension(int num) {
    /// <summary>
    /// Writes the decimal representation of the <see cref="int"/> value
    /// into the provided span as ASCII bytes, without allocating a string.
    /// </summary>
    /// <param name="sp">
    /// The destination span; must have enough capacity.
    /// </param>
    /// <returns>
    /// The number of bytes written, or 0 if the span is too small.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the value is negative, a leading hyphen is written, and the
    /// absolute value is then converted. The span length must be at least the
    ///  required digit count plus one (for negative numbers) to succeed.
    /// If the span is insufficient, 0 is returned and nothing is written.
    /// </para>
    /// <para>
    /// This method is allocation‑free and inlined for performance.
    /// </para>
    /// </remarks>
#pragma warning disable S6640 // Unsafe code blocks should not be used
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe byte ToAscii(Span<byte> sp) {
      if (sp.IsEmpty) return 0;

      if (num < 0) {
        fixed (byte* ptr = sp)
          ptr[0] = Ascii.HyphenMinus;

        uint ne = unchecked((uint)-num);
        byte res = ne.ToAscii(sp[1..]);
        return res is 0 ? (byte)0 : (byte)(res + 1);
      }

      return ((uint)num).ToAscii(sp);
    }
#pragma warning restore S6640 // Unsafe code blocks should not be used
  }

  extension(uint num) {
    /// <summary>
    /// Writes the decimal representation of the <see cref="uint"/> value
    /// into the provided span as ASCII bytes, without allocating a string.
    /// </summary>
    /// <param name="sp">
    /// The destination span; must have enough capacity.
    /// </param>
    /// <returns>
    /// The number of bytes written, or 0 if the span is too small.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses the fast <see cref="CountDigits"/> to determine the
    /// length, then fills the span from right to left using the
    /// <see cref="TwoDigit"/> lookup table for groups of two digits.
    /// For values less than 100, it handles single‑digit or two‑digit cases
    /// directly.
    /// </para>
    /// <para>
    /// The conversion is allocation‑free and inlined.
    /// If the span length is insufficient,
    /// 0 is returned and no data is written.
    /// </para>
    /// </remarks>
#pragma warning disable S6640 // Unsafe code blocks should not be used
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe byte ToAscii(Span<byte> sp) {
      if (sp.IsEmpty) return 0;

      if (num < 10) {
        sp[0] = (byte)(Ascii.Zero + num);
        return 1;
      }

      int len = CountDigits(num);
      if (len > sp.Length) return 0;

      var LUT = TwoDigit;

      fixed (byte* pdest = sp) {
        var ptr = pdest + len;

        while (num >= 100) {
          ptr -= 2;
          (num, uint idx) = Math.DivRem(num, 100);
          idx *= 2;
          ptr[0] = LUT[(int)idx];
          ptr[1] = LUT[(int)idx + 1];
        }

        if (num < 10)
          ptr[-1] = (byte)(Ascii.Zero + num);
        else {
          int idx = (int)(num * 2);
          ptr[-2] = LUT[idx];
          ptr[-1] = LUT[idx + 1];
        }
      }

      return (byte)len;
    }
#pragma warning restore S6640 // Unsafe code blocks should not be used
  }
}
