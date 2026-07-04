
using System;
using System.Numerics;
using Core;

namespace Test;

public class NumericTests {
  [Fact]
  public void ZeroIfLessThan_EmptySpan_DoesNothing() {
    Span<float> data = [];
    Numeric.ZeroIfLessThan(data, 0.5f);
  }

  [Fact]
  public void ZeroIfLessThan_VectorizedPath_Works() {
    int vecSize = Vector<float>.Count;
    int length = vecSize * 2 + 3;
    Span<float> data = new float[length];

    for (int i = 0; i < length; i++) {
      data[i] = i % 3 == 0 ? 0.5f : 2.0f;
    }

    float threshold = 1.0f;
    Numeric.ZeroIfLessThan(data, threshold);

    for (int i = 0; i < length; i++) {
      if (i % 3 == 0)
        Assert.Equal(0f, data[i]);
      else
        Assert.Equal(2.0f, data[i]);
    }
  }

  [Fact]
  public void ZeroIfLessThanAligned_LengthMultipleOfVectorSize_Works() {
    int vecSize = Vector<float>.Count;
    int len = vecSize * 3;
    var data = new float[len];
    for (int i = 0; i < len; i++)
      data[i] = (i % 2 == 0) ? 2.0f : -0.5f;

    Numeric.ZeroIfLessThanAligned(data.AsSpan(), 0.0f);

    for (int i = 0; i < len; i++) {
      if (i % 2 == 0)
        Assert.Equal(2.0f, data[i]);
      else
        Assert.Equal(0f, data[i]);
    }
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
