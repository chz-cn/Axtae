// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System.Numerics;
using System.Runtime.CompilerServices;

using static Axtae.Random.IRandom;

namespace Axtae.Random;

/// <summary>
/// A Xoroshiro128+ pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoroshiro128+ is a fast, lightweight PRNG with a 128-bit state.
/// The output is the sum of the two state elements (s0 + s1).
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/> and is suitable for
/// high-performance scenarios where 64-bit output quality is sufficient.
/// </para>
/// </remarks>
public struct Xoroshiro128Plus : IRandom<Xoroshiro128Plus> {
  /// <summary>Left shift applied to s1 during the state update.</summary>
  public const int ShiftS1 = 16;

  /// <summary>Left rotation applied to s0 during the state update.</summary>
  public const int RotateS0 = 24;

  /// <summary>Left rotation applied to s1 during the state update.</summary>
  public const int RotateS1 = 37;

  /// <inheritdoc/>
  public static Xoroshiro128Plus Create () => new (SplitMix64.NewSeed ());

  private ulong _s0, _s1;

  /// <summary>
  /// Initializes a new <see cref="Xoroshiro128Plus"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoroshiro128Plus (ulong seed) {
    SplitMix64 rand = new (seed);
    this._s0 = rand.NextUInt64 ();
    this._s1 = rand.NextUInt64 ();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoroshiro128Plus"/> instance with explicit
  /// state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <remarks>
  /// If both state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoroshiro128Plus (ulong s0, ulong s1) {
    (this._s0, this._s1) = (s0 | s1) is 0
      ? (GoldenRatio, MixConst1)
      : (s0, s1);
  }

  /// <inheritdoc/>
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64 () {
    var (s0, s1) = (this._s0, this._s1);

    ulong result = unchecked(s0 + s1);
    s1 ^= s0;

    this._s0 = BitOperations.RotateLeft (s0, RotateS0) ^ s1 ^ (s1 << ShiftS1);
    this._s1 = BitOperations.RotateLeft (s1, RotateS1);

    return result;
  }
}

/// <summary>
/// A Xoroshiro128++ pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoroshiro128++ is a variant that uses a rotated-sum-plus-state output
/// function for improved statistical quality.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/>.
/// </para>
/// </remarks>
public struct Xoroshiro128PlusPlus : IRandom<Xoroshiro128PlusPlus> {
  /// <summary>Left shift applied to s1 during the state update.</summary>
  public const int ShiftS1 = 21;

  /// <summary>Left rotation applied to s0 during the state update.</summary>
  public const int RotateS0 = 49;

  /// <summary>Left rotation applied to s1 during the state update.</summary>
  public const int RotateS1 = 28;

  /// <summary>Left rotation applied to the s0 + s1 output sum.</summary>
  public const int OutputRotate = 17;

  /// <inheritdoc/>
  public static Xoroshiro128PlusPlus Create () => new (SplitMix64.NewSeed ());

  private ulong _s0, _s1;

  /// <summary>
  /// Initializes a new <see cref="Xoroshiro128PlusPlus"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoroshiro128PlusPlus (ulong seed) {
    SplitMix64 rand = new (seed);
    this._s0 = rand.NextUInt64 ();
    this._s1 = rand.NextUInt64 ();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoroshiro128PlusPlus"/> instance with
  /// explicit state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <remarks>
  /// If both state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoroshiro128PlusPlus (ulong s0, ulong s1) {
    (this._s0, this._s1) = (s0 | s1) is 0
      ? (GoldenRatio, MixConst1)
      : (s0, s1);
  }

  /// <inheritdoc/>
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64 () {
    var (s0, s1) = (this._s0, this._s1);

    ulong result = unchecked(
      BitOperations.RotateLeft (s0 + s1, OutputRotate) + s0
    );
    s1 ^= s0;

    this._s0 = BitOperations.RotateLeft (s0, RotateS0) ^ s1 ^ (s1 << ShiftS1);
    this._s1 = BitOperations.RotateLeft (s1, RotateS1);

    return result;
  }
}

/// <summary>
/// A Xoroshiro128** pseudo-random number generator.
/// </summary>
/// <remarks>
/// <para>
/// Xoroshiro128** is a variant that uses a multiplication-based output
/// function for improved statistical quality.
/// </para>
/// <para>
/// This struct implements <see cref="IRandom"/>.
/// </para>
/// </remarks>
public struct Xoroshiro128StarStar : IRandom<Xoroshiro128StarStar> {
  /// <summary>Left shift applied to s1 during the state update.</summary>
  public const int ShiftS1 = 16;

  /// <summary>Left rotation applied to s0 during the state update.</summary>
  public const int RotateS0 = 24;

  /// <summary>Left rotation applied to s1 during the state update.</summary>
  public const int RotateS1 = 37;

  /// <summary>Left rotation applied to the multiplied output.</summary>
  public const int OutputRotate = 7;

  /// <summary>First multiplier applied to s0 in the output function.</summary>
  public const ulong OutputMul1 = 5;

  /// <summary>
  /// Second multiplier applied after the rotation in the output function.
  /// </summary>
  public const ulong OutputMul2 = 9;

  /// <inheritdoc/>
  public static Xoroshiro128StarStar Create () => new (SplitMix64.NewSeed ());

  private ulong _s0, _s1;

  /// <summary>
  /// Initializes a new <see cref="Xoroshiro128StarStar"/> instance with the
  /// specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the initial state via SplitMix64.
  /// </param>
  public Xoroshiro128StarStar (ulong seed) {
    SplitMix64 rand = new (seed);
    this._s0 = rand.NextUInt64 ();
    this._s1 = rand.NextUInt64 ();
  }

  /// <summary>
  /// Initializes a new <see cref="Xoroshiro128StarStar"/> instance with
  /// explicit state values.
  /// </summary>
  /// <param name="s0">State element 0.</param>
  /// <param name="s1">State element 1.</param>
  /// <remarks>
  /// If both state values are zero, they are replaced with non-zero default
  /// values.
  /// </remarks>
  public Xoroshiro128StarStar (ulong s0, ulong s1) {
    (this._s0, this._s1) = (s0 | s1) is 0
      ? (GoldenRatio, MixConst1)
      : (s0, s1);
  }

  /// <inheritdoc/>
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64 () {
    var (s0, s1) = (this._s0, this._s1);

    ulong result = unchecked(
      BitOperations.RotateLeft (s0 * OutputMul1, OutputRotate) * OutputMul2
    );
    s1 ^= s0;

    this._s0 = BitOperations.RotateLeft (s0, RotateS0) ^ s1 ^ (s1 << ShiftS1);
    this._s1 = BitOperations.RotateLeft (s1, RotateS1);

    return result;
  }
}
