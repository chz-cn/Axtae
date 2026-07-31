
using System.Runtime.CompilerServices;
using static Axtae.Random.IRandom;

namespace Axtae.Random;

/// <summary>
/// A SplitMix64 pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// SplitMix64 is a simple, fast PRNG suitable for generating initial state values
/// for other generators (such as Xoshiro or Xoroshiro). It is based on the
/// splitmix64 algorithm by Sebastiano Vigna.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/> and can be used as a standalone
/// generator or for seeding other generators.
/// </para>
/// </remarks>
public struct SplitMix64(ulong x) : IRandom {
  private ulong _state = x;

  /// <inheritdoc/>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    ulong z = this._state += GoldenRatio;
    z = (z ^ (z >> 30)) * MixConst1;
    z = (z ^ (z >> 27)) * MixConst2;
    return z ^ (z >> 31);
  }

  /// <summary>
  /// Mixes the state value and returns a random 64-bit result, updating the
  /// state in-place.
  /// </summary>
  /// <param name="state">
  /// The state value to mix; will be incremented by <see cref="GoldenRatio"/>.
  /// </param>
  /// <returns>A 64-bit mixed result.</returns>
  public static ulong Mix(ref ulong state) {
    ulong z = state += GoldenRatio;
    z = (z ^ (z >> 30)) * MixConst1;
    z = (z ^ (z >> 27)) * MixConst2;
    return z ^ (z >> 31);
  }

  /// <summary>
  /// Mixes the given state value and returns a random 64-bit result without
  /// modifying the original state.
  /// </summary>
  /// <param name="state">The state value to mix.</param>
  /// <returns>A 64-bit mixed result.</returns>
  public static ulong Mix(ulong state) {
    ulong z = state + GoldenRatio;
    z = (z ^ (z >> 30)) * MixConst1;
    z = (z ^ (z >> 27)) * MixConst2;
    return z ^ (z >> 31);
  }
}
