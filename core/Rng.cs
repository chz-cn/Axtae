
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Core.Random;

using static Core.Random.IRandom;

namespace Core;

#pragma warning disable S3453 // Classes should not have only "private"
// constructors
public sealed class Rng : IRandom {
#pragma warning restore S3453 // Classes should not have only "private"
  // constructors
  public static readonly Rng Shared
    = new((ulong)Guid.NewGuid().GetHashCode());

  private readonly Lock _lock = new();

#pragma warning disable S2933 // Fields that are only assigned in the
  // constructor should be "readonly"
  private ulong _s0, _s1, _s2, _s3;
#pragma warning restore S2933 // Fields that are only assigned in the
  // constructor should be "readonly"

#pragma warning disable S1144 // Unused private types or members should
  // be removed
  private Rng(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();
    this._s2 = mix.NextUInt64();
    this._s3 = mix.NextUInt64();

    if ((this._s0 | this._s1 | this._s2 | this._s3) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
    }
  }
#pragma warning restore S1144 // Unused private types or members should be
  // removed

  public ulong NextUInt64() {
    lock (this._lock)
      return this.Next();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ulong Next() {
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
