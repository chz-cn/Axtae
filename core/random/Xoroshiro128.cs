
using System.Numerics;
using System.Runtime.CompilerServices;
using static Core.Random.IRandom;

namespace Core.Random;

public struct Xoroshiro128Plus : IRandom {
  private ulong _s0, _s1;

  public Xoroshiro128Plus(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();

    if ((this._s0 | this._s1) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
    }
  }

  public Xoroshiro128Plus(ulong s0, ulong s1) {
    (this._s0, this._s1) = (s0, s1);

    if ((s0 | s1) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    var (s0, s1) = (this._s0, this._s1);
    ulong result = s0 + s1;

    s1 ^= s0;
    this._s0 = BitOperations.RotateLeft(s0, RotateS0) ^ s1 ^ (s1 << ShiftS1);
    this._s1 = BitOperations.RotateLeft(s1, RotateS1);

    return result;
  }
}

public struct Xoroshiro128PlusPlus : IRandom {
  private ulong _s0, _s1;

  public Xoroshiro128PlusPlus(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();

    if ((this._s0 | this._s1) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
    }
  }

  public Xoroshiro128PlusPlus(ulong s0, ulong s1) {
    (this._s0, this._s1) = (s0, s1);

    if ((this._s0 | this._s1) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    var (s0, s1) = (this._s0, this._s1);

    ulong result = BitOperations.RotateLeft(s0 + s1, 17) + s0;

    s1 ^= s0;
    this._s0 = BitOperations.RotateLeft(s0, RotateS0) ^ s1 ^ (s1 << ShiftS1);
    this._s1 = BitOperations.RotateLeft(s1, RotateS1);

    return result;
  }
}

public struct Xoroshiro128StarStar : IRandom {
  private ulong _s0, _s1;

  public Xoroshiro128StarStar(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();

    if ((this._s0 | this._s1) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
    }
  }

  public Xoroshiro128StarStar(ulong s0, ulong s1) {
    (this._s0, this._s1) = (s0, s1);

    if ((this._s0 | this._s1) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    var (s0, s1) = (this._s0, this._s1);

    ulong result = BitOperations.RotateLeft(s0 * 5, 7) * 9;

    s1 ^= s0;
    this._s0 = BitOperations.RotateLeft(s0, RotateS0) ^ s1 ^ (s1 << ShiftS1);
    this._s1 = BitOperations.RotateLeft(s1, RotateS1);

    return result;
  }
}
