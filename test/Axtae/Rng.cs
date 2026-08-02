
using System;
using Axtae;
using Axtae.Random;

namespace Test;

public sealed class RngTests {
  [Fact]
  public void Fill_ULong_Empty_DoesNothing() {
    Rng.Shared.Fill([]);

    Assert.True(true);
  }

  [Fact]
  public void Shared_ReturnsInRange() {
    var rng = Rng.Shared;
    for (int i = 0; i < 100; i++)
      Assert.InRange(rng.NextUInt64(), ulong.MinValue, ulong.MaxValue);
  }

  // ===== exts =====
  [Fact]
  public void NextUInt64_Max_ZeroReturnsZero() {
    var rng = Rng.Shared;
    Assert.Equal(0UL, rng.NextUInt64(0));
  }

  [Fact]
  public void NextUInt64_Max_ReturnsInRange() {
    var rng = Rng.Shared;
    for (ulong max = 1; max <= 10; max++)
      for (int i = 0; i < 20; i++) {
        ulong v = rng.NextUInt64(max);
        Assert.InRange(v, 0u, max - 1);
      }
  }

  [Fact]
  public void NextUInt64_Max_Rejection_Sampling() {
    var rng = Rng.Shared;
    const ulong Max = (ulong.MaxValue / 2) + 2;

    for (int i = 0; i < 50; i++) {
      ulong v = rng.NextUInt64(Max);
      Assert.InRange(v, 0u, Max - 1);
    }
  }

  [Fact]
  public void NextUInt64_MinMax_InvalidRangeReturnsZero() {
    var rng = Rng.Shared;
    Assert.Equal(0u, rng.NextUInt64(5, 3));
    Assert.Equal(0u, rng.NextUInt64(5, 5));
  }

  [Fact]
  public void NextUInt64_MinMax_ReturnsInRange() {
    var rng = Rng.Shared;

    for (ulong min = 0; min < 5; min++)
      for (ulong max = min + 1; max <= min + 10; max++)
        for (int i = 0; i < 20; i++)
          Assert.InRange(rng.NextUInt64(min, max), min, max - 1);
  }

  [Fact]
  public void NextDouble_ReturnsInRange() {
    var rng = Rng.Shared;
    for (int i = 0; i < 50; i++) {
      var d = rng.NextDouble();
      Assert.InRange(d, 0, 1);
      Assert.NotEqual(1, d);
    }
  }

  [Fact]
  public void NextDoubleInclusive_ReturnsInRange() {
    var rng = Rng.Shared;

    for (int i = 0; i < 50; i++)
      Assert.InRange(rng.NextDoubleInclusive(), 0, 1);
  }

  // ===== Fill =====
  [Fact]
  public void Fill_Ulong_EmptyReturnsImmediately() {
    var rand = Rng.Shared;
    Span<ulong> empty = [];
    rand.Fill(empty);

    Assert.True(empty.IsEmpty);
  }

  [Fact]
  public void Fill_Ulong_FillsWithRandomValues() {
    var rand = Rng.Shared;
    Span<ulong> buffer = stackalloc ulong[10];
    rand.Fill(buffer);

    bool all_zero = true;
    foreach (var v in buffer)
      if (v != 0) { all_zero = false; break; }

    Assert.False(all_zero, "All filled values were zero.");
  }

  [Fact]
  public void Fill_Generic_Int_FillsCorrectly() {
    var rand = Rng.Shared;
    Span<int> buffer = stackalloc int[10];
    rand.Fill(buffer);

    bool all_zero = true;
    foreach (var v in buffer)
      if (v != 0) { all_zero = false; break; }

    Assert.False(all_zero, "All filled values were zero.");
  }

  [Fact]
  public void Fill_Generic_Float_FillsCorrectly() {
    var rand = Rng.Shared;
    Span<float> buffer = stackalloc float[10];
    rand.Fill(buffer);

    bool all_zero = true;
    foreach (var v in buffer)
      if (v != 0f) { all_zero = false; break; }

    Assert.False(all_zero, "All filled values were zero.");
  }

  [Fact]
  public void Fill_Generic_Struct_FillsCorrectly() {
    var rand = Rng.Shared;
    Span<Random.Point> buffer = stackalloc Random.Point[5];
    rand.Fill(buffer);

    bool all_zero = true;
    foreach (var p in buffer)
      if (p.X != 0 || p.Y != 0) { all_zero = false; break; }

    Assert.False(all_zero, "All filled values were zero.");
  }

  [Fact]
  public void Fill_Generic_EmptyReturnsImmediately() {
    var rand = Rng.Shared;
    Span<int> empty = [];
    rand.Fill(empty);

    Assert.True(empty.IsEmpty);
  }

  [Fact]
  public void Fill_Unmanaged_Remaining_1_to_7() {
    var rand = Rng.Shared;
    Span<byte> buffer = stackalloc byte[7];

    rand.Fill(buffer);
    rand.Fill(buffer[..6]);
    rand.Fill(buffer[..5]);
    rand.Fill(buffer[..4]);
    rand.Fill(buffer[..3]);
    rand.Fill(buffer[..2]);
    rand.Fill(buffer[..1]);

    Assert.True(true);
  }
}
