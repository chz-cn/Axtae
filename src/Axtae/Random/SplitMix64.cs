// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.CompilerServices;

using static Axtae.Random.IRandom;

namespace Axtae.Random;

/// <summary>
/// A SplitMix64 pseudo-random number generator.
/// </summary>
/// <param name="x">The initial state value of the generator.</param>
/// <remarks>
/// <para>
/// SplitMix64 is a simple, fast PRNG suitable for generating initial state
/// values for other generators (such as Xoshiro or Xoroshiro). It is based on
/// the SplitMix64 algorithm by Sebastiano Vigna.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/> and can be used as a
/// standalone generator or for seeding other generators.
/// </para>
/// </remarks>
public struct SplitMix64 (ulong x) : IRandom<SplitMix64> {
  [ThreadStatic]
  private static ulong _s;

  /// <summary>
  /// Returns a new seed value from a thread-local, lazily initialized
  /// <see cref="SplitMix64"/> stream.
  /// </summary>
  /// <returns>
  /// A pseudo-random <see cref="ulong"/> suitable for seeding another
  /// generator.
  /// </returns>
  /// <remarks>
  /// <para>
  /// The thread-local state is initialized on first use from a
  /// <see cref="Guid"/>-derived value, then advanced on every call via
  /// <see cref="Mix(ref ulong)"/>. Successive calls on the same thread therefore
  /// return independent values, while different threads keep separate
  /// streams.
  /// </para>
  /// <para>
  /// Not cryptographically secure. Intended only for non-adversarial
  /// seeding of non-cryptographic generators.
  /// </para>
  /// </remarks>
  public static ulong NewSeed () {
    if (_s is 0) {
      var guid = Guid.NewGuid ();
      _s = Unsafe.As<Guid, ulong> (ref guid);
    }

    return Mix (ref _s);
  }

  /// <inheritdoc/>
  public static SplitMix64 Create () => new (NewSeed ());

  private ulong _state = x;

  /// <inheritdoc/>
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64 () {
    ulong z = this._state += GoldenRatio;
    z = (z ^ (z >>> 30)) * MixConst1;
    z = (z ^ (z >>> 27)) * MixConst2;
    return z ^ (z >>> 31);
  }

  /// <summary>
  /// Mixes the state value and returns a random 64-bit result, updating the
  /// state in-place.
  /// </summary>
  /// <param name="state">
  /// The state value to mix; will be incremented by <see cref="GoldenRatio"/>.
  /// </param>
  /// <returns>A 64-bit mixed result.</returns>
  public static ulong Mix (ref ulong state) {
    ulong z = state += GoldenRatio;
    z = (z ^ (z >>> 30)) * MixConst1;
    z = (z ^ (z >>> 27)) * MixConst2;
    return z ^ (z >>> 31);
  }

  /// <summary>
  /// Mixes the given state value and returns a random 64-bit result without
  /// modifying the original state.
  /// </summary>
  /// <param name="state">The state value to mix.</param>
  /// <returns>A 64-bit mixed result.</returns>
  public static ulong Mix (ulong state) {
    ulong z = state + GoldenRatio;
    z = (z ^ (z >>> 30)) * MixConst1;
    z = (z ^ (z >>> 27)) * MixConst2;
    return z ^ (z >>> 31);
  }
}
