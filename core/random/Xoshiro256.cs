
using System.Numerics;
using System.Runtime.CompilerServices;
using static Core.Random.IRandom;

namespace Core.Random;

public struct Xoshiro256Plus : IRandom {
  private ulong _s0, _s1, _s2, _s3;

  public Xoshiro256Plus(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();
    this._s2 = mix.NextUInt64();
    this._s3 = mix.NextUInt64();

    if ((this._s0 | this._s1 | this._s2 | this._s3) == 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
    }
  }

  public Xoshiro256Plus(ulong s0, ulong s1, ulong s2, ulong s3) {
    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);

    if ((this._s0 | this._s1 | this._s2 | this._s3) == 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    var (s0, s1, s2, s3) = (this._s0, this._s1, this._s2, this._s3);

    ulong result = s0 + s3;

    ulong t = s1 << ShiftS1;
    s2 ^= s0;
    s3 ^= s1;
    s1 ^= s2;
    s0 ^= s3;
    s2 ^= t;
    s3 = BitOperations.RotateLeft(s3, RotateS3);

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    return result;
  }
}

public struct Xoshiro256PlusPlus : IRandom {
  private ulong _s0, _s1, _s2, _s3;

  public Xoshiro256PlusPlus(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();
    this._s2 = mix.NextUInt64();
    this._s3 = mix.NextUInt64();

    if ((this._s0 | this._s1 | this._s2 | this._s3) == 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
    }
  }

  public Xoshiro256PlusPlus(ulong s0, ulong s1, ulong s2, ulong s3) {
    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);

    if ((this._s0 | this._s1 | this._s2 | this._s3) == 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    var (s0, s1, s2, s3) = (this._s0, this._s1, this._s2, this._s3);

    ulong result = BitOperations.RotateLeft(s0 + s3, 23) + s0;

    ulong t = s1 << ShiftS1;
    s2 ^= s0;
    s3 ^= s1;
    s1 ^= s2;
    s0 ^= s3;
    s2 ^= t;
    s3 = BitOperations.RotateLeft(s3, RotateS3);

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    return result;
  }
}

public struct Xoshiro256StarStar : IRandom {
  private ulong _s0, _s1, _s2, _s3;

  public Xoshiro256StarStar(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();
    this._s2 = mix.NextUInt64();
    this._s3 = mix.NextUInt64();

    if ((this._s0 | this._s1 | this._s2 | this._s3) == 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
    }
  }

  public Xoshiro256StarStar(ulong s0, ulong s1, ulong s2, ulong s3) {
    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);

    if ((this._s0 | this._s1 | this._s2 | this._s3) == 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    var (s0, s1, s2, s3) = (this._s0, this._s1, this._s2, this._s3);

    ulong result = BitOperations.RotateLeft(s1 * 5, 7) * 9;

    ulong t = s1 << ShiftS1;
    s2 ^= s0;
    s3 ^= s1;
    s1 ^= s2;
    s0 ^= s3;
    s2 ^= t;
    s3 = BitOperations.RotateLeft(s3, RotateS3);

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    return result;
  }
}
