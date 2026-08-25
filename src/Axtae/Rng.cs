
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Axtae.Random;

using static Axtae.Random.IRandom;

namespace Axtae;

/// <summary>
/// A thread-safe random number generator based on the Xoshiro256** algorithm.
/// </summary>
/// <remarks>
/// Non-cryptographic, suitable for simulations, sampling, games.
/// Thread-safety via <see cref="ThreadStaticAttribute"/> per-thread singleton.
/// </remarks>
public sealed class Rng : IRandom {
  /// <summary>
  /// Per-thread shared instance. Lazy-initialized with GUID-derived seed.
  /// </summary>
  /// <value>
  /// The per-thread shared instance of the <see cref="Rng"/>.
  /// </value>
  public static Rng Shared => _rand ?? Create();

  [ThreadStatic]
  private static Rng? _rand;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static Rng Create() => _rand = new();

  private ulong _s0, _s1, _s2, _s3;

  private Rng() {
    SplitMix64 rand = new((ulong)Guid.NewGuid().GetHashCode());
    this._s0 = rand.NextUInt64();
    this._s1 = rand.NextUInt64();
    this._s2 = rand.NextUInt64();
    this._s3 = rand.NextUInt64();
  }

  /// <inheritdoc/>
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
