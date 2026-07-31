
using System.Numerics;
using System.Runtime.CompilerServices;
using static Axtae.Random.IRandom;

namespace Axtae.Random;

/// <summary>
/// A Xoshiro256+ pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoshiro256+ is a fast, lightweight PRNG with a 256-bit state.
/// The output is the sum of state elements s0 and s3.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/> and is suitable for
/// high-performance scenarios where 64-bit output quality is sufficient.
/// </para>
/// </remarks>
public struct Xoshiro256Plus : IRandom {
  private ulong _s0, _s1, _s2, _s3;

  /// <summary>
  /// Initializes a new <see cref="Xoshiro256Plus"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoshiro256Plus(ulong seed) {
    SplitMix64 rand = new(seed);
    this._s0 = rand.NextUInt64();
    this._s1 = rand.NextUInt64();
    this._s2 = rand.NextUInt64();
    this._s3 = rand.NextUInt64();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoshiro256Plus"/> instance with explicit
  /// state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <param name="s2">State element 2.</param>
  /// <param name="s3">State element 3.</param>
  /// <remarks>
  /// If all state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoshiro256Plus(ulong s0, ulong s1, ulong s2, ulong s3)
    => (this._s0, this._s1, this._s2, this._s3) = (s0 | s1 | s2 | s3) is 0
      ? (GoldenRatio, MixConst1, MixConst2, GoldenRatio)
      : (s0, s1, s2, s3);

  /// <inheritdoc/>
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

/// <summary>
/// A Xoshiro256++ pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoshiro256++ is a variant that uses a rotated-sum-plus-state output
/// function for improved statistical quality.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/>.
/// </para>
/// </remarks>
public struct Xoshiro256PlusPlus : IRandom {
  private ulong _s0, _s1, _s2, _s3;

  /// <summary>
  /// Initializes a new <see cref="Xoshiro256PlusPlus"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoshiro256PlusPlus(ulong seed) {
    SplitMix64 rand = new(seed);
    this._s0 = rand.NextUInt64();
    this._s1 = rand.NextUInt64();
    this._s2 = rand.NextUInt64();
    this._s3 = rand.NextUInt64();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoshiro256PlusPlus"/> instance with
  /// explicit state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <param name="s2">State element 2.</param>
  /// <param name="s3">State element 3.</param>
  /// <remarks>
  /// If all state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoshiro256PlusPlus(ulong s0, ulong s1, ulong s2, ulong s3)
    => (this._s0, this._s1, this._s2, this._s3) = (s0 | s1 | s2 | s3) is 0
      ? (GoldenRatio, MixConst1, MixConst2, GoldenRatio)
      : (s0, s1, s2, s3);

  /// <inheritdoc/>
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

/// <summary>
/// A Xoshiro256** pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoshiro256** is a variant that uses a multiplication-based output function
/// for improved statistical quality. It is the recommended variant for most
/// general-purpose use.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/>.
/// </para>
/// </remarks>
public struct Xoshiro256StarStar : IRandom {
  private ulong _s0, _s1, _s2, _s3;

  /// <summary>
  /// Initializes a new <see cref="Xoshiro256StarStar"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoshiro256StarStar(ulong seed) {
    SplitMix64 rand = new(seed);
    this._s0 = rand.NextUInt64();
    this._s1 = rand.NextUInt64();
    this._s2 = rand.NextUInt64();
    this._s3 = rand.NextUInt64();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoshiro256StarStar"/> instance with
  /// explicit state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <param name="s2">State element 2.</param>
  /// <param name="s3">State element 3.</param>
  /// <remarks>
  /// If all state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoshiro256StarStar(ulong s0, ulong s1, ulong s2, ulong s3)
    => (this._s0, this._s1, this._s2, this._s3) = (s0 | s1 | s2 | s3) is 0
      ? (GoldenRatio, MixConst1, MixConst2, GoldenRatio)
      : (s0, s1, s2, s3);

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
