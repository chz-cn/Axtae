
using System;
using System.Runtime.CompilerServices;

namespace Axtae.Encode;

public static class Ascii {
  public const byte NUL = 0;
  public const byte SOH = 1;
  public const byte STX = 2;
  public const byte ETX = 3;
  public const byte EOT = 4;
  public const byte ENQ = 5;
  public const byte ACK = 6;
  public const byte BEL = 7;
  public const byte Backspace = 8;
  public const byte HT = 9;
  public const byte LF = 10;
  public const byte VT = 11;
  public const byte FF = 12;
  public const byte CR = 13;
  public const byte SO = 14;
  public const byte SI = 15;
  public const byte DLE = 16;
  public const byte DC1 = 17;
  public const byte DC2 = 18;
  public const byte DC3 = 19;
  public const byte DC4 = 20;
  public const byte NAK = 21;
  public const byte SYN = 22;
  public const byte ETB = 23;
  public const byte CAN = 24;
  public const byte EM = 25;
  public const byte SUB = 26;
  public const byte ESC = 27;
  public const byte FS = 28;
  public const byte GS = 29;
  public const byte RS = 30;
  public const byte US = 31;

  public const byte Space = 32;
  public const byte ExclamationMark = (byte)'!';
  public const byte QuotationMark = (byte)'"';
  public const byte NumberSign = (byte)'#';
  public const byte DollarSign = (byte)'$';
  public const byte PercentSign = (byte)'%';
  public const byte Ampersand = (byte)'&';
  public const byte Apostrophe = (byte)'\'';
  public const byte OpenParenthesis = (byte)'(';
  public const byte CloseParenthesis = (byte)')';
  public const byte Asterisk = (byte)'*';
  public const byte PlusSign = (byte)'+';
  public const byte Comma = (byte)',';
  public const byte HyphenMinus = (byte)'-';
  public const byte Period = (byte)'.';
  public const byte Slash = (byte)'/';

  // numbers
  public const byte Zero = (byte)'0';
  public const byte One = (byte)'1';
  public const byte Two = (byte)'2';
  public const byte Three = (byte)'3';
  public const byte Four = (byte)'4';
  public const byte Five = (byte)'5';
  public const byte Six = (byte)'6';
  public const byte Seven = (byte)'7';
  public const byte Eight = (byte)'8';
  public const byte Nine = (byte)'9';

  public const byte Colon = (byte)':';
  public const byte Semicolon = (byte)';';
  public const byte LessThan = (byte)'<';
  public const byte EqualSign = (byte)'=';
  public const byte GreaterThan = (byte)'>';
  public const byte QuestionMark = (byte)'?';
  public const byte AtSign = (byte)'@';

  // A-Z
  public const byte A = (byte)'A';
  public const byte B = (byte)'B';
  public const byte C = (byte)'C';
  public const byte D = (byte)'D';
  public const byte E = (byte)'E';
  public const byte F = (byte)'F';
  public const byte G = (byte)'G';
  public const byte H = (byte)'H';
  public const byte I = (byte)'I';
  public const byte J = (byte)'J';
  public const byte K = (byte)'K';
  public const byte L = (byte)'L';
  public const byte M = (byte)'M';
  public const byte N = (byte)'N';
  public const byte O = (byte)'O';
  public const byte P = (byte)'P';
  public const byte Q = (byte)'Q';
  public const byte R = (byte)'R';
  public const byte S = (byte)'S';
  public const byte T = (byte)'T';
  public const byte U = (byte)'U';
  public const byte V = (byte)'V';
  public const byte W = (byte)'W';
  public const byte X = (byte)'X';
  public const byte Y = (byte)'Y';
  public const byte Z = (byte)'Z';

  public const byte OpenBracket = (byte)'[';
  public const byte Backslash = (byte)'\\';
  public const byte CloseBracket = (byte)']';
  public const byte Caret = (byte)'^';
  public const byte Underscore = (byte)'_';
  public const byte Backtick = (byte)'`';

#pragma warning disable IDE1006 // 命名样式
  // a-z
  public const byte a = (byte)'a';
  public const byte b = (byte)'b';
  public const byte c = (byte)'c';
  public const byte d = (byte)'d';
  public const byte e = (byte)'e';
  public const byte f = (byte)'f';
  public const byte g = (byte)'g';
  public const byte h = (byte)'h';
  public const byte i = (byte)'i';
  public const byte j = (byte)'j';
  public const byte k = (byte)'k';
  public const byte l = (byte)'l';
  public const byte m = (byte)'m';
  public const byte n = (byte)'n';
  public const byte o = (byte)'o';
  public const byte p = (byte)'p';
  public const byte q = (byte)'q';
  public const byte r = (byte)'r';
  public const byte s = (byte)'s';
  public const byte t = (byte)'t';
  public const byte u = (byte)'u';
  public const byte v = (byte)'v';
  public const byte w = (byte)'w';
  public const byte x = (byte)'x';
  public const byte y = (byte)'y';
  public const byte z = (byte)'z';
#pragma warning restore IDE1006 // 命名样式

  public const byte OpenBrace = (byte)'{';
  public const byte Pipe = (byte)'|';
  public const byte CloseBrace = (byte)'}';
  public const byte Tilde = (byte)'~';
  public const byte DEL = 127;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable S6640 // Unsafe code blocks should not be used
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
