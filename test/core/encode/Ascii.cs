
using System;
using Core.Encode;

namespace Test.Encode;

public sealed class AsciiTests {
  public static readonly TheoryData<uint, int> CountDigitsData = new() {
    { 1u, 1}, {uint.MaxValue, 10 },
    { 10u, 2}, {999999999u, 9 },
    { 100u, 3}, {99999999u, 8 },
    { 1000u, 4}, {9999999u, 7 },
    { 10000u, 5}, {999999u, 6 },
    { 100000u, 6}, {99999u, 5 },
    { 1000000u, 7}, {9999u, 4 },
    { 10000000u, 8}, {999u, 3 },
    { 100000000u, 9}, {99u, 2 },
    { 1000000000u, 10}, {9u, 1 }
  };

  [Theory]
  [MemberData(nameof(CountDigitsData))]
  public void CountDigits_ReturnsCorrectDigitCount(uint value, int expectedDigits) {
    Assert.Equal(expectedDigits, Ascii.CountDigits(value));
  }

  public static readonly TheoryData<uint, string> UInt32ToAsciiData = new() {
    { 0u, "0" },
    { 1u, "1" },
    { 10u, "10" },
    { 99u, "99" },
    { 100u, "100" },
    { 999u, "999" },
    { 1234u, "1234" },
    { 99999u, "99999" },
    { 1234567890u,"1234567890" },
    { 4294967295u, "4294967295" }
  };

  [Theory]
  [MemberData(nameof(UInt32ToAsciiData))]
  public void ToAscii_UInt_ReturnsCorrectBytes(uint value, string expectedString) {
    Span<byte> buffer = stackalloc byte[10];
    byte len = value.ToAscii(buffer);

    string actual = System.Text.Encoding.UTF8.GetString(buffer[..len]);

    Assert.Equal(expectedString.Length, len);
    Assert.Equal(expectedString, actual);
  }

  [Fact]
  public void ToAscii_UInt_BufferTooShort_ReturnsZero() {
    uint value = 12345;
    Span<byte> buffer = stackalloc byte[3];
    byte len = Ascii.ToAscii(value, buffer);
    Assert.Equal(0, len);

  }

  [Fact]
  public void ToAscii_UInt_EmptyBuffer_ReturnsZero() {
    Span<byte> buffer = Span<byte>.Empty;
    byte len = Ascii.ToAscii(123, buffer);
    Assert.Equal(0, len);
  }

  [Theory]
  [InlineData(0, "0")]
  [InlineData(5, "5")]
  [InlineData(-5, "-5")]
  [InlineData(123, "123")]
  [InlineData(-123, "-123")]
  [InlineData(int.MaxValue, "2147483647")]
  [InlineData(int.MinValue, "-2147483648")]
  public void ToAscii_Int_ReturnsCorrectBytes(int value, string expectedString) {
    Span<byte> buffer = stackalloc byte[16];
    byte len = Ascii.ToAscii(value, buffer);
    Assert.Equal(expectedString.Length, len);
    string actual = System.Text.Encoding.UTF8.GetString(buffer[..len]);
    Assert.Equal(expectedString, actual);
  }

  [Fact]
  public void ToAscii_Int_Negative_WithBufferJustEnough() {
    int value = -123;
    Span<byte> buffer = stackalloc byte[4];
    byte len = Ascii.ToAscii(value, buffer);
    Assert.Equal(4, len);
    Assert.Equal("-123", System.Text.Encoding.UTF8.GetString(buffer));
  }

  [Fact]
  public void ToAscii_Int_BufferTooShort_ReturnsZero() {
    int value = -12345;
    Span<byte> buffer = stackalloc byte[4];
    byte len = Ascii.ToAscii(value, buffer);
    Assert.Equal(0, len);
  }

  [Fact]
  public void ToAscii_Int_EmptyBuffer_ReturnsZero() {
    Span<byte> buffer = Span<byte>.Empty;
    byte len = Ascii.ToAscii(42, buffer);
    Assert.Equal(0, len);
  }
}