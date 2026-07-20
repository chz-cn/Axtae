
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Axtae.Random;

using static Axtae.Random.IRandom;

namespace Axtae;

public sealed class Rng : IRandom {
  public static readonly Rng Shared
    = new((ulong)Guid.NewGuid().GetHashCode());

  private readonly Lock _lock = new();

  private ulong _s0, _s1, _s2, _s3;

  private Rng(ulong seed) {
    SplitMix64 mix = new(seed);
    this._s0 = mix.NextUInt64();
    this._s1 = mix.NextUInt64();
    this._s2 = mix.NextUInt64();
    this._s3 = mix.NextUInt64();
  }

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
