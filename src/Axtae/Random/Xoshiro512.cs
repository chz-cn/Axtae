
using System.Numerics;
using System.Runtime.CompilerServices;
using static Axtae.Random.IRandom;

namespace Axtae.Random;

/// <summary>
/// A Xoshiro512+ pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoshiro512+ is a fast, lightweight PRNG with a 512-bit state.
/// The output is the sum of state elements s0 and s7.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/> and is suitable for
/// high-performance scenarios where a larger state space is desired.
/// </para>
/// </remarks>
public struct Xoshiro512Plus : IRandom {
  private ulong _s0, _s1, _s2, _s3, _s4, _s5, _s6, _s7;

  /// <summary>
  /// Initializes a new <see cref="Xoshiro512Plus"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoshiro512Plus(ulong seed) {
    SplitMix64 rand = new(seed);
    this._s0 = rand.NextUInt64();
    this._s1 = rand.NextUInt64();
    this._s2 = rand.NextUInt64();
    this._s3 = rand.NextUInt64();
    this._s4 = rand.NextUInt64();
    this._s5 = rand.NextUInt64();
    this._s6 = rand.NextUInt64();
    this._s7 = rand.NextUInt64();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoshiro512Plus"/> instance with explicit
  /// state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <param name="s2">State element 2.</param>
  /// <param name="s3">State element 3.</param>
  /// <param name="s4">State element 4.</param>
  /// <param name="s5">State element 5.</param>
  /// <param name="s6">State element 6.</param>
  /// <param name="s7">State element 7.</param>
  /// <remarks>
  /// If all state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoshiro512Plus(ulong s0, ulong s1, ulong s2, ulong s3,
    ulong s4, ulong s5, ulong s6, ulong s7) {
    if ((s0 | s1 | s2 | s3 | s4 | s5 | s6 | s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
      return;
    }

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);
  }

  /// <inheritdoc/>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

/// <summary>
/// A Xoshiro512++ pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoshiro512++ is a variant that uses a rotated-sum-plus-state output
/// function for improved statistical quality.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/>.
/// </para>
/// </remarks>
public struct Xoshiro512PlusPlus : IRandom {
  private ulong _s0, _s1, _s2, _s3, _s4, _s5, _s6, _s7;

  /// <summary>
  /// Initializes a new <see cref="Xoshiro512PlusPlus"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoshiro512PlusPlus(ulong seed) {
    SplitMix64 rand = new(seed);
    this._s0 = rand.NextUInt64();
    this._s1 = rand.NextUInt64();
    this._s2 = rand.NextUInt64();
    this._s3 = rand.NextUInt64();
    this._s4 = rand.NextUInt64();
    this._s5 = rand.NextUInt64();
    this._s6 = rand.NextUInt64();
    this._s7 = rand.NextUInt64();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoshiro512PlusPlus"/> instance with
  /// explicit state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <param name="s2">State element 2.</param>
  /// <param name="s3">State element 3.</param>
  /// <param name="s4">State element 4.</param>
  /// <param name="s5">State element 5.</param>
  /// <param name="s6">State element 6.</param>
  /// <param name="s7">State element 7.</param>
  /// <remarks>
  /// If all state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoshiro512PlusPlus(ulong s0, ulong s1, ulong s2, ulong s3,
    ulong s4, ulong s5, ulong s6, ulong s7) {
    if ((s0 | s1 | s2 | s3 | s4 | s5 | s6 | s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
      return;
    }

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);
  }

  /// <inheritdoc/>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

/// <summary>
/// A Xoshiro512** pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoshiro512** is a variant that uses a multiplication-based output function
/// for improved statistical quality.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/>.
/// </para>
/// </remarks>
public struct Xoshiro512StarStar : IRandom {
  private ulong _s0, _s1, _s2, _s3, _s4, _s5, _s6, _s7;

  /// <summary>
  /// Initializes a new <see cref="Xoshiro512StarStar"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoshiro512StarStar(ulong seed) {
    SplitMix64 rand = new(seed);
    this._s0 = rand.NextUInt64();
    this._s1 = rand.NextUInt64();
    this._s2 = rand.NextUInt64();
    this._s3 = rand.NextUInt64();
    this._s4 = rand.NextUInt64();
    this._s5 = rand.NextUInt64();
    this._s6 = rand.NextUInt64();
    this._s7 = rand.NextUInt64();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoshiro512StarStar"/> instance with
  /// explicit state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <param name="s2">State element 2.</param>
  /// <param name="s3">State element 3.</param>
  /// <param name="s4">State element 4.</param>
  /// <param name="s5">State element 5.</param>
  /// <param name="s6">State element 6.</param>
  /// <param name="s7">State element 7.</param>
  /// <remarks>
  /// If all state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoshiro512StarStar(ulong s0, ulong s1, ulong s2, ulong s3,
    ulong s4, ulong s5, ulong s6, ulong s7) {
    if ((s0 | s1 | s2 | s3 | s4 | s5 | s6 | s7) is 0) {
      this._s0 = GoldenRatio;
      this._s1 = MixConst1;
      this._s2 = MixConst2;
      this._s3 = GoldenRatio;
      this._s4 = MixConst1;
      this._s5 = MixConst2;
      this._s6 = GoldenRatio;
      this._s7 = MixConst1;
      return;
    }

    (this._s0, this._s1, this._s2, this._s3) = (s0, s1, s2, s3);
    (this._s4, this._s5, this._s6, this._s7) = (s4, s5, s6, s7);
  }

  /// <inheritdoc/>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
