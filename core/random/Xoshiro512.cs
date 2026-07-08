
using System.Numerics;
using static Core.Random.IRandom;

namespace Core.Random;

public struct Xoshiro512Plus : IRandom {
#pragma warning disable S2933 // Fields that are only assigned in the
  // constructor should be "readonly"
  private ulong _s0, _s1, _s2, _s3, _s4, _s5, _s6, _s7;
#pragma warning restore S2933 // Fields that are only assigned in the
  // constructor should be "readonly"

  public Xoshiro512Plus(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();
    this._s2 = mix.NextUInt64();
    this._s3 = mix.NextUInt64();
    this._s4 = mix.NextUInt64();
    this._s5 = mix.NextUInt64();
    this._s6 = mix.NextUInt64();
    this._s7 = mix.NextUInt64();

    if ((this._s0 | this._s1 | this._s2 | this._s3
      | this._s4 | this._s5 | this._s6 | this._s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
    }
  }

  public Xoshiro512Plus(ulong s0, ulong s1, ulong s2, ulong s3,
    ulong s4, ulong s5, ulong s6, ulong s7) {
    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);

    if ((this._s0 | this._s1 | this._s2 | this._s3 |
      this._s4 | this._s5 | this._s6 | this._s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
    }
  }

  public ulong NextUInt64() {
    var (s0, s1, s2, s3) = (this._s0, this._s1, this._s2, this._s3);
    var (s4, s5, s6, s7) = (this._s4, this._s5, this._s6, this._s7);

    ulong result = s0 + s7;

    ulong t = s1 << ShiftS1;

    s7 ^= s0;
    s3 ^= s1;
    s1 ^= s2;
    s0 ^= s3;
    s2 ^= s4;
    s5 ^= s6;
    s4 ^= s7;
    s6 ^= t;
    s7 = BitOperations.RotateLeft(s7, RotateS7);

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);
    return result;
  }
}

public struct Xoshiro512PlusPlus : IRandom {
#pragma warning disable S2933 // Fields that are only assigned in the
  // constructor should be "readonly"
  private ulong _s0, _s1, _s2, _s3, _s4, _s5, _s6, _s7;
#pragma warning restore S2933 // Fields that are only assigned in the
  // constructor should be "readonly"

  public Xoshiro512PlusPlus(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();
    this._s2 = mix.NextUInt64();
    this._s3 = mix.NextUInt64();
    this._s4 = mix.NextUInt64();
    this._s5 = mix.NextUInt64();
    this._s6 = mix.NextUInt64();
    this._s7 = mix.NextUInt64();

    if ((this._s0 | this._s1 | this._s2 | this._s3
      | this._s4 | this._s5 | this._s6 | this._s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
    }
  }

  public Xoshiro512PlusPlus(ulong s0, ulong s1, ulong s2, ulong s3,
    ulong s4, ulong s5, ulong s6, ulong s7) {
    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);

    if ((this._s0 | this._s1 | this._s2 | this._s3
      | this._s4 | this._s5 | this._s6 | this._s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
    }
  }

  public ulong NextUInt64() {
    var (s0, s1, s2, s3) = (this._s0, this._s1, this._s2, this._s3);
    var (s4, s5, s6, s7) = (this._s4, this._s5, this._s6, this._s7);

    ulong result = BitOperations.RotateLeft(s0 + s7, 17) + s0;

    ulong t = s1 << ShiftS1;

    s7 ^= s0;
    s3 ^= s1;
    s1 ^= s2;
    s0 ^= s3;
    s2 ^= s4;
    s5 ^= s6;
    s4 ^= s7;
    s6 ^= t;
    s7 = BitOperations.RotateLeft(s7, RotateS7);

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);

    return result;
  }
}

public struct Xoshiro512StarStar : IRandom {
#pragma warning disable S2933 // Fields that are only assigned in the
  // constructor should be "readonly"
  private ulong _s0, _s1, _s2, _s3, _s4, _s5, _s6, _s7;
#pragma warning restore S2933 // Fields that are only assigned in the
  // constructor should be "readonly"

  public Xoshiro512StarStar(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();
    this._s2 = mix.NextUInt64();
    this._s3 = mix.NextUInt64();
    this._s4 = mix.NextUInt64();
    this._s5 = mix.NextUInt64();
    this._s6 = mix.NextUInt64();
    this._s7 = mix.NextUInt64();

    if ((this._s0 | this._s1 | this._s2 | this._s3
      | this._s4 | this._s5 | this._s6 | this._s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
    }
  }

  public Xoshiro512StarStar(ulong s0, ulong s1, ulong s2, ulong s3,
    ulong s4, ulong s5, ulong s6, ulong s7) {
    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);

    if ((this._s0 | this._s1 | this._s2 | this._s3
      | this._s4 | this._s5 | this._s6 | this._s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
    }
  }

  public ulong NextUInt64() {
    var (s0, s1, s2, s3) = (this._s0, this._s1, this._s2, this._s3);
    var (s4, s5, s6, s7) = (this._s4, this._s5, this._s6, this._s7);

    ulong result = BitOperations.RotateLeft(s0 * 5, 7) * 9;

    ulong t = s1 << ShiftS1;

    s7 ^= s0;
    s3 ^= s1;
    s1 ^= s2;
    s0 ^= s3;
    s2 ^= s4;
    s5 ^= s6;
    s4 ^= s7;
    s6 ^= t;
    s7 = BitOperations.RotateLeft(s7, RotateS7);

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);

    return result;
  }
}
