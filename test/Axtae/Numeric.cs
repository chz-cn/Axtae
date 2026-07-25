
using System;
using System.Numerics;
using Axtae;

namespace Test;

public sealed class NumericTests {
  [Fact]
  public void ZeroIfLessThan_EmptySpan_DoesNothing() {
    Span<float> data = [];
    Numeric.ZeroIfLessThan(data, 0.5f);
    Assert.True(data is []);
  }

  [Fact]
  public void ZeroIfLessThan_VectorizedPath_Works() {
    int vec_size = Vector<float>.Count;
    int length = (vec_size * 2) + 3;
    Span<float> data = stackalloc float[length];

    for (int i = 0; i < length; i++)
      data[i] = i % 3 is 0 ? 0.5f : 2.0f;

    const float Threshold = 1.0f;
    Numeric.ZeroIfLessThan(data, Threshold);

    for (int i = 0; i < length; i++)
      if (i % 3 is 0) Assert.Equal(0f, data[i]);
      else Assert.Equal(2.0f, data[i]);
  }

  [Fact]
  public void ZeroIfLessThanAligned_LengthMultipleOfVectorSize_Works() {
    int vec_size = Vector<float>.Count;
    int len = vec_size * 3;
    Span<float> data = stackalloc float[len];
    for (int i = 0; i < len; i++)
      data[i] = (i % 2 is 0) ? 2.0f : -0.5f;

    Numeric.ZeroIfLessThanAligned(data, 0.0f);

    for (int i = 0; i < len; i++)
      if (i % 2 is 0) Assert.Equal(2.0f, data[i]);
      else Assert.Equal(0f, data[i]);
  }

  [Theory]
  [InlineData(0u, 0u, 0u)]
  [InlineData(1u, 1u, 0u)]
  [InlineData(0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFE)]
  public void MulHi_UInt_ReturnsHigh32Bits(uint a, uint b, uint expected) {
    uint result = Numeric.MulHi(a, b);
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData(0ul, 0ul, 0ul)]
  [InlineData(1ul, 1ul, 0ul)]
  [InlineData(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFE)]
  public void MulHi_ULong_ReturnsHigh64Bits(ulong a, ulong b, ulong expected) {
    ulong result = Numeric.MulHi(a, b);
    Assert.Equal(expected, result);
  }
}
