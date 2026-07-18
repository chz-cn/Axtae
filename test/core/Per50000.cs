
using Core;

namespace Test;

public sealed class Per50000Tests {
  [Theory]
  [InlineData(1000, 0, 1000)]
  [InlineData(1000, 25000, 500)]
  [InlineData(1000, 50000, 0)]
  [InlineData(0, 12345, 0)]
  [InlineData(12345, 12345, 9297)]
  [InlineData(uint.MaxValue, 0, uint.MaxValue)]
  public void CalcDI_ReturnsExpected(uint damage, ushort di, uint expected) {
    uint result = Per50000.CalcDI(damage, di);
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData(1000, 0, 0, 1000)]
  [InlineData(1000, 25000, 10000, 600)]
  [InlineData(1000, 50000, 0, 0)]
  [InlineData(1000, 0, 50000, 2000)]
  [InlineData(1000, 25000, 25000, 750)]
  [InlineData(uint.MaxValue, 0, 0, uint.MaxValue)]
  public void CalcDIV_ReturnsExpected(uint damage, ushort dr, ushort vul, uint expected) {
    uint result = Per50000.CalcDIV(damage, dr, vul);
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData(1000, 0, 0, 0, 1000)]
  [InlineData(1000, 25000, 25000, 10000, 300)]
  [InlineData(1000, 50000, 0, 0, 0)]
  [InlineData(1000, 0, 50000, 0, 0)]
  [InlineData(1000, 0, 0, 50000, 2000)]
  [InlineData(1000, 25000, 0, 25000, 750)]
  [InlineData(0, 25000, 25000, 25000, 0)]
  public void CalcDamage_ReturnsExpected(uint damage, ushort di, ushort dr, ushort vul, uint expected) {
    uint result = Per50000.CalcDamage(damage, di, dr, vul);
    Assert.Equal(expected, result);
  }

  [Fact]
  public void CalcDI_DI_GreaterThanOne_ProducesUnexpectedResult() {
    uint result = Per50000.CalcDI(1000, 60000);

    Assert.NotEqual(0u, result);
    Assert.NotEqual(1000u, result);
  }

  [Fact]
  public void CalcDIV_Vul_ExceedsOne_BehavesAsExpected() {
    uint result = Per50000.CalcDIV(1000, 0, 60000);

    Assert.Equal(2200u, result);
  }
}
