// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;

using Axtae;

using Xunit;

namespace Test;

public sealed class NumericTests {
  [Theory]
  [InlineData (0u, 0u, 0u, 0u)]
  [InlineData (1u, 1u, 0u, 1u)]
  [InlineData (0x10000u, 0x10000u, 1u, 0u)]
  [InlineData (0x80000000u, 2u, 1u, 0u)]
  [InlineData (0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFEu, 1u)]
  public void Mul_UInt_ReturnsHighAndLow (
    uint a, uint b, uint expected_high, uint expected_low) {
    (uint high, uint low) = Numeric.Mul (a, b);
    Assert.Equal (expected_high, high);
    Assert.Equal (expected_low, low);
  }

  [Theory]
  [InlineData (123456789u, 987654321u)]
  [InlineData (0xDEADBEEFu, 0xCAFEBABEu)]
  [InlineData (0xFFFFFFFFu, 0xFFFFFFFFu)]
  public void Mul_UInt_MatchesBigMul (uint a, uint b) {
    ulong product = Math.BigMul (a, b);
    (uint high, uint low) = Numeric.Mul (a, b);
    Assert.Equal ((uint)(product >>> 32), high);
    Assert.Equal ((uint)product, low);
  }

  [Theory]
  [InlineData (0ul, 0ul, 0ul, 0ul)]
  [InlineData (1ul, 1ul, 0ul, 1ul)]
  [InlineData (0x100000000ul, 0x100000000ul, 1ul, 0ul)]
  [InlineData (0x123456789ABCDEF0ul, 0x10ul, 1ul, 0x23456789ABCDEF00ul)]
  [InlineData (ulong.MaxValue, ulong.MaxValue, ulong.MaxValue - 1, 1ul)]
  public void Mul_ULong_ReturnsHighAndLow (
    ulong a,
    ulong b,
    ulong expected_high,
    ulong expected_low
  ) {
    (ulong high, ulong low) = Numeric.Mul (a, b);
    Assert.Equal (expected_high, high);
    Assert.Equal (expected_low, low);
  }

  [Theory]
  [InlineData (123456789ul, 987654321ul)]
  [InlineData (0xDEADBEEFDEADBEEFul, 0xCAFEBABECAFEBABEul)]
  [InlineData (ulong.MaxValue, ulong.MaxValue)]
  public void Mul_ULong_MatchesBigMul (ulong a, ulong b) {
    ulong expected_high = Math.BigMul (a, b, out ulong expected_low);
    (ulong high, ulong low) = Numeric.Mul (a, b);
    Assert.Equal (expected_high, high);
    Assert.Equal (expected_low, low);
  }
}
