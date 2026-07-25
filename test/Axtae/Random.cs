
using Axtae.Random;

namespace Test;

public static class Random {
  public const ulong Seed0 = 12345;
  public const ulong Seed1 = 0xDEADBEEF;
}

public abstract class RandomTests<T> where T : struct, IRandom, allows ref struct {
  protected abstract T Create(ulong seed);

  [Fact]
  public void NextUInt64_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);
    for (int i = 0; i < 100; i++)
      Assert.InRange(rng.NextUInt64(), ulong.MinValue, ulong.MaxValue);
  }

  [Fact]
  public void NextUInt64_NotAllZero() {
    var rng = this.Create(Random.Seed0);
    bool all_zero = true;
    for (int i = 0; i < 20; i++)
      if (rng.NextUInt64() != 0) { all_zero = false; break; }

    Assert.False(all_zero, "All values were zero, algorithm likely broken.");
  }

  [Fact]
  public void NextUInt64_Deterministic() {
    var a = this.Create(Random.Seed0);
    var b = this.Create(Random.Seed0);
    for (int i = 0; i < 10; i++)
      Assert.Equal(a.NextUInt64(), b.NextUInt64());
  }

  [Fact]
  public void NextUInt64_DifferentSeeds_DifferentSequence() {
    var a = this.Create(Random.Seed0);
    var b = this.Create(Random.Seed1);
    bool any_diff = false;
    for (int i = 0; i < 10; i++)
      if (a.NextUInt64() != b.NextUInt64()) { any_diff = true; break; }

    Assert.True(any_diff, "Different seeds produced identical sequence.");
  }

  // ===== exts =====
  [Fact]
  public void NextUInt64_Max_ZeroReturnsZero() {
    var rng = this.Create(Random.Seed0);
    Assert.Equal(0UL, rng.NextUInt64(0));
  }

  [Fact]
  public void NextUInt64_Max_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);
    for (ulong max = 1; max <= 10; max++)
      for (int i = 0; i < 20; i++) {
        ulong v = rng.NextUInt64(max);
        Assert.InRange(v, 0u, max - 1);
      }
  }

  [Fact]
  public void NextUInt64_Max_Rejection_Sampling() {
    var rng = this.Create(Random.Seed0);
    const ulong Max = (ulong.MaxValue / 2) + 2;

    for (int i = 0; i < 50; i++) {
      ulong v = rng.NextUInt64(Max);
      Assert.InRange(v, 0u, Max - 1);
    }
  }

  [Fact]
  public void NextUInt64_MinMax_InvalidRangeReturnsZero() {
    var rng = this.Create(Random.Seed0);
    Assert.Equal(0u, rng.NextUInt64(5, 3));
    Assert.Equal(0u, rng.NextUInt64(5, 5));
  }

  [Fact]
  public void NextUInt64_MinMax_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);

    for (ulong min = 0; min < 5; min++)
      for (ulong max = min + 1; max <= min + 10; max++)
        for (int i = 0; i < 20; i++)
          Assert.InRange(rng.NextUInt64(min, max), min, max - 1);
  }

  [Fact]
  public void NextDouble_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);
    for (int i = 0; i < 50; i++) {
      var d = rng.NextDouble();
      Assert.InRange(d, 0, 1);
      Assert.NotEqual(1, d);
    }
  }

  [Fact]
  public void NextDoubleInclusive_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);

    for (int i = 0; i < 50; i++)
      Assert.InRange(rng.NextDoubleInclusive(), 0, 1);
  }
}


public abstract class RandomClassTests<T> where T : class, IRandom {
  protected abstract T Create(ulong seed);

  [Fact]
  public void NextUInt64_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);
    for (int i = 0; i < 100; i++)
      Assert.InRange(rng.NextUInt64(), ulong.MinValue, ulong.MaxValue);
  }

  [Fact]
  public void NextUInt64_NotAllZero() {
    var rng = this.Create(Random.Seed0);
    bool all_zero = true;
    for (int i = 0; i < 20; i++)
      if (rng.NextUInt64() != 0) { all_zero = false; break; }

    Assert.False(all_zero, "All values were zero, algorithm likely broken.");
  }

  [Fact]
  public void NextUInt64_Deterministic() {
    var a = this.Create(Random.Seed0);
    var b = this.Create(Random.Seed0);
    for (int i = 0; i < 10; i++)
      Assert.Equal(a.NextUInt64(), b.NextUInt64());
  }

  [Fact]
  public void NextUInt64_DifferentSeeds_DifferentSequence() {
    var a = this.Create(Random.Seed0);
    var b = this.Create(Random.Seed1);
    bool any_diff = false;
    for (int i = 0; i < 10; i++)
      if (a.NextUInt64() != b.NextUInt64()) { any_diff = true; break; }

    Assert.True(any_diff, "Different seeds produced identical sequence.");
  }

  // ===== exts =====
  [Fact]
  public void NextUInt64_Max_ZeroReturnsZero() {
    var rng = this.Create(Random.Seed0);
    Assert.Equal(0UL, rng.NextUInt64(0));
  }

  [Fact]
  public void NextUInt64_Max_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);
    for (ulong max = 1; max <= 10; max++)
      for (int i = 0; i < 20; i++) {
        ulong v = rng.NextUInt64(max);
        Assert.InRange(v, 0u, max - 1);
      }
  }

  [Fact]
  public void NextUInt64_Max_Rejection_Sampling() {
    var rng = this.Create(Random.Seed0);
    const ulong Max = (ulong.MaxValue / 2) + 2;

    for (int i = 0; i < 50; i++) {
      ulong v = rng.NextUInt64(Max);
      Assert.InRange(v, 0u, Max - 1);
    }
  }

  [Fact]
  public void NextUInt64_MinMax_InvalidRangeReturnsZero() {
    var rng = this.Create(Random.Seed0);
    Assert.Equal(0u, rng.NextUInt64(5, 3));
    Assert.Equal(0u, rng.NextUInt64(5, 5));
  }

  [Fact]
  public void NextUInt64_MinMax_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);

    for (ulong min = 0; min < 5; min++)
      for (ulong max = min + 1; max <= min + 10; max++)
        for (int i = 0; i < 20; i++)
          Assert.InRange(rng.NextUInt64(min, max), min, max - 1);
  }

  [Fact]
  public void NextDouble_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);
    for (int i = 0; i < 50; i++) {
      var d = rng.NextDouble();
      Assert.InRange(d, 0, 1);
      Assert.NotEqual(1, d);
    }
  }

  [Fact]
  public void NextDoubleInclusive_ReturnsInRange() {
    var rng = this.Create(Random.Seed0);

    for (int i = 0; i < 50; i++)
      Assert.InRange(rng.NextDoubleInclusive(), 0, 1);
  }
}


public sealed class SplitMix64Tests : RandomTests<SplitMix64> {
  protected override SplitMix64 Create(ulong seed) => new(seed);

  [Fact]
  public void Mix2Times_DoesNotSae() {
    var rand = SplitMix64.Mix(Random.Seed0);
    var res = SplitMix64.Mix(ref rand);
    Assert.NotEqual(rand, res);
  }
}

public sealed class Xoroshiro128PlusTests : RandomTests<Xoroshiro128Plus> {
  protected override Xoroshiro128Plus Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoroshiro128Plus(0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoroshiro128Plus(1, 2);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Xoroshiro128PlusPlusTests
  : RandomTests<Xoroshiro128PlusPlus> {
  protected override Xoroshiro128PlusPlus Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoroshiro128PlusPlus(0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoroshiro128PlusPlus(1, 2);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Xoroshiro128StarStarTests
  : RandomTests<Xoroshiro128StarStar> {
  protected override Xoroshiro128StarStar Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoroshiro128StarStar(0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoroshiro128StarStar(1, 2);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Xoshiro256PlusTests : RandomTests<Xoshiro256Plus> {
  protected override Xoshiro256Plus Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoshiro256Plus(0, 0, 0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoshiro256Plus(1, 2, 3, 4);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Xoshiro256PlusPlusTests : RandomTests<Xoshiro256PlusPlus> {
  protected override Xoshiro256PlusPlus Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoshiro256PlusPlus(0, 0, 0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoshiro256PlusPlus(1, 2, 3, 4);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Xoshiro256StarStarTests : RandomTests<Xoshiro256StarStar> {
  protected override Xoshiro256StarStar Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoshiro256StarStar(0, 0, 0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoshiro256StarStar(1, 2, 3, 4);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Xoshiro512PlusTests : RandomTests<Xoshiro512Plus> {
  protected override Xoshiro512Plus Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoshiro512Plus(0, 0, 0, 0, 0, 0, 0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoshiro512Plus(1, 2, 3, 4, 5, 6, 7, 8);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Xoshiro512PlusPlusTests : RandomTests<Xoshiro512PlusPlus> {
  protected override Xoshiro512PlusPlus Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoshiro512PlusPlus(0, 0, 0, 0, 0, 0, 0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoshiro512PlusPlus(1, 2, 3, 4, 5, 6, 7, 8);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Xoshiro512StarStarTests : RandomTests<Xoshiro512StarStar> {
  protected override Xoshiro512StarStar Create(ulong seed) => new(seed);

  [Fact]
  public void Create_WithAllZero_DoesNotMake0() {
    var rng = new Xoshiro512StarStar(0, 0, 0, 0, 0, 0, 0, 0);
    Assert.NotEqual(0u, rng.NextUInt64());
  }

  [Fact]
  public void Create_WithFullParam_DoesNotMake0() {
    var rng = new Xoshiro512StarStar(1, 2, 3, 4, 5, 6, 7, 8);
    Assert.NotEqual(0u, rng.NextUInt64());
  }
}

public sealed class Philox4x32Tests : RandomClassTests<Philox4x32> {
  protected override Philox4x32 Create(ulong seed) => new(seed);

  [Fact]
  public void JumpTo_ChangesState() {
    var rng = new Philox4x32(Random.Seed0);
    var before = rng.NextUInt64();

    rng.JumpTo(1, 2, 3, 4);
    var after = rng.NextUInt64();
    Assert.NotEqual(before, after);
  }

  [Fact]
  public void JumpTo_Max_DoesNotMake0() {
    const uint Max = uint.MaxValue;
    ulong val;
    var rng = new Philox4x32(1, 2, 3, 4, 5, 6);

    val = rng.NextUInt64();
    Assert.NotEqual(0u, val);

    rng.JumpTo(Max, 2, 3, 4);
    val = rng.NextUInt64();
    Assert.NotEqual(0u, val);

    rng.JumpTo(Max, Max, 3, 4);
    val = rng.NextUInt64();
    Assert.NotEqual(0u, val);

    rng.JumpTo(Max, Max, Max, 4);
    val = rng.NextUInt64();
    Assert.NotEqual(0u, val);
  }
}

public sealed class Philox4x64Tests : RandomClassTests<Philox4x64> {
  protected override Philox4x64 Create(ulong seed) => new(seed);

  [Fact]
  public void JumpTo_ChangesState() {
    var rng = new Philox4x64(Random.Seed0);
    var before = rng.NextUInt64();

    rng.JumpTo(1, 2, 3, 4);
    var after = rng.NextUInt64();
    Assert.NotEqual(before, after);
  }

  [Fact]
  public void JumpTo_Max_DoesNotMake0() {
    const ulong Max = ulong.MaxValue;
    ulong val;
    var rng = new Philox4x64(1, 2, 3, 4, 5, 6);

    val = rng.NextUInt64();
    Assert.NotEqual(0u, val);

    rng.JumpTo(Max, 2, 3, 4);
    val = rng.NextUInt64();
    Assert.NotEqual(0u, val);

    rng.JumpTo(Max, Max, 3, 4);
    val = rng.NextUInt64();
    Assert.NotEqual(0u, val);

    rng.JumpTo(Max, Max, Max, 4);
    val = rng.NextUInt64();
    Assert.NotEqual(0u, val);
  }
}
