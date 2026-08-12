
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Axtae.Random;

/// <summary>
/// Defines the contract for a pseudo-random number generator that produces
/// 64-bit unsigned integers.
/// </summary>
/// <remarks>
/// <para>
/// Implementations can be either structs (for deterministic, allocation-free
/// usage) or classes (for shared, thread-safe usage).
/// <b>To avoid boxing of <see langword="struct"/> implementations,
/// always use a generic constraint with <see langword="ref"/> T
/// <see langword="where"/> T : <see cref="IRandom"/></b>
/// when passing <see langword="struct"/> instances as parameters.
/// Extension methods in <see cref="IRandomExtensions"/> follow this pattern.
/// </para>
/// <para>
/// Implementations should provide a deterministic sequence of pseudo-random
/// numbers based on an internal state.
/// The constants defined in this interface (e.g., <see cref="GoldenRatio"/>,
/// <see cref="MixConst1"/>, and various shift values) are intended for use in
/// common mixing functions, such as the SplitMix64 algorithm, and can be
/// reused by implementers.
/// </para>
/// </remarks>
#pragma warning disable S4136 // Method overloads should be grouped together
public interface IRandom {
  /// <summary>
  /// The golden ratio constant, often used as an additive constant in linear
  /// congruential generators and mixing functions.
  /// </summary>
  const ulong GoldenRatio = 0x9E3779B97F4A7C15;

  /// <summary>
  /// First mixing constant for the SplitMix64 finalizer.
  /// </summary>
  const ulong MixConst1 = 0xBF58476D1CE4E5B9;

  /// <summary>
  /// Second mixing constant for the SplitMix64 finalizer.
  /// </summary>
  const ulong MixConst2 = 0x94D049BB133111EB;

  /// <summary>
  /// Rotate amount used in the first stage of the mixing function.
  /// </summary>
  const int RotateS0 = 24;

  /// <summary>
  /// Rotate amount for state element 0 in Xoshiro / Xoroshiro generators
  /// (first stage of mixing).
  /// </summary>
  const int RotateS1 = 37;

  /// <summary>
  /// Rotate amount for state element 3 in Xoshiro256 generators
  /// (third stage of mixing).
  /// </summary>
  const int RotateS3 = 45;

  /// <summary>
  /// Rotate amount for state element 7 in Xoshiro512 generators
  /// (final stage of mixing).
  /// </summary>
  const int RotateS7 = 29;

  /// <summary>
  /// Left shift amount for state element 1 in Xoshiro/Xoroshiro generators.
  /// </summary>
  const int ShiftS1 = 17;

  /// <summary>
  /// Number of bits to shift right when converting a 64-bit value to a
  /// <see cref="double"/> in the range [0, 1).
  /// </summary>
  const int DoubleShift = 11;

  /// <summary>
  /// Number of bits of precision used for <see cref="double"/> conversion.
  /// </summary>
  const int DoublePrecision = 53;

  /// <summary>
  /// Scaling factor to convert a 53-bit integer to a <see cref="double"/>
  /// in [0, 1).
  /// </summary>
  const double DoubleScale = 1.0 / (1UL << DoublePrecision);

  /// <summary>
  /// Generates a uniformly distributed 64-bit unsigned integer.
  /// </summary>
  /// <returns>A pseudo-random <see cref="ulong"/> value.</returns>
  ulong NextUInt64();

  #region T where T : struct, IRandom, allows ref struct

  /// <summary>
  /// Returns a random 64-bit unsigned integer in the range
  /// [0, <paramref name="max"/>).
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <param name="max">
  /// The exclusive upper bound. Must be greater than 0.
  /// </param>
  /// <returns> A random <see cref="ulong"/> by Generated. </returns>
  /// <remarks>
  /// <para>
  /// The method uses the unbiased rejection sampling technique to avoid
  /// modulo bias.
  /// It repeatedly generates random values until one falls below the
  /// rejection threshold,
  /// then returns the remainder modulo <paramref name="max"/>.
  /// </para>
  /// <para>
  /// If <paramref name="max"/> is 0, the method returns 0 without
  /// generating any random numbers.
  /// </para>
  /// </remarks>
  static virtual ulong NextUInt64<T>(ref T rand, ulong max)
    where T : struct, IRandom, allows ref struct {
    if (max is 0) return 0;
    ulong threshold = unchecked((0ul - max) % max);
    while (true) {
      ulong r = rand.NextUInt64();
      if (r >= threshold) return r % max;
    }
  }

  /// <summary>
  /// Returns a random 64-bit unsigned integer in the range
  /// [<paramref name="min"/>, <paramref name="max"/>).
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <param name="min">The inclusive lower bound.</param>
  /// <param name="max">
  /// The exclusive upper bound. Must be greater than <paramref name="min"/>.
  /// </param>
  /// <returns> A random <see cref="ulong"/> by Generated.</returns>
  /// <remarks>
  /// If <paramref name="max"/> is less than <paramref name="min"/>,
  /// the method returns 0 without generating random  numbers.
  /// </remarks>
  static virtual ulong NextUInt64<T>(ref T rand, ulong min, ulong max)
    where T : struct, IRandom, allows ref struct {
    if (min >= max) return 0;
    var range = max - min;
    return min + rand.NextUInt64(range);
  }

  /// <summary>
  /// Returns a random <see cref="double"/> in the range [0, 1).
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <returns>A double in [0, 1).</returns>
  /// <remarks>
  /// The value is generated by shifting the 64-bit random value right by
  /// <see cref="DoubleShift"/> bits to obtain a 53-bit integer,
  /// then multiplying by <see cref="DoubleScale"/>.
  /// This yields a uniform distribution with 53 bits of precision.
  /// </remarks>
  static virtual double NextDouble<T>(ref T rand)
    where T : struct, IRandom, allows ref struct
    => (rand.NextUInt64() >> DoubleShift) * DoubleScale;

  /// <summary>
  /// Returns a random <see cref="double"/> in the range [0, 1].
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <returns>A double in [0, 1], inclusive of 1.</returns>
  /// <remarks>
  /// The value is obtained by dividing the full 64-bit random integer by
  /// <see cref="ulong.MaxValue"/>. This gives a uniform distribution over
  /// all possible <see cref="double"/> values in that range, though the
  /// granularity is limited by the representation of <see cref="double"/>.
  /// </remarks>
  static virtual double NextDoubleInclusive<T>(ref T rand)
    where T : struct, IRandom, allows ref struct
    => rand.NextUInt64() / (double)ulong.MaxValue;

  /// <summary>
  /// Fills the elements of a <see cref="Span{T}"/> with random
  /// <see cref="ulong"/> values.
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <param name="buffer">
  /// The span to fill. If empty, the method returns immediately.
  /// </param>
  static virtual void Fill<T>(ref T rand, scoped Span<ulong> buffer)
    where T : struct, IRandom, allows ref struct {
    if (buffer.IsEmpty) return;
    foreach (ref var value in buffer)
      value = rand.NextUInt64();
  }

  /// <summary>
  /// Fills the elements of a <see cref="Span{T}"/> with random values of
  /// any <see langword="unmanaged"/> type.
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <typeparam name="U">The unmanaged element type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <param name="buffer">
  /// The span to fill. If empty, the method returns immediately.
  /// </param>
  /// <remarks>
  /// The method converts the span to a byte sequence,
  /// fills complete 8-byte chunks as <see cref="ulong"/> values,
  /// then copies any remaining bytes from an extra random value
  /// using a <c>switch</c> with fallthrough <c>goto case</c> statements.
  /// </remarks>
  static virtual void Fill<T, U>(ref T rand, scoped Span<U> buffer)
    where T : struct, IRandom, allows ref struct
    where U : unmanaged {
    if (buffer.IsEmpty) return;

    var bytes = MemoryMarshal.AsBytes(buffer);
    var sp = MemoryMarshal.Cast<byte, ulong>(bytes);
    rand.Fill(sp);

    int remaining = bytes.Length % 8;
    if (remaining is 0) return;

    ulong last = rand.NextUInt64();
    ref byte src = ref Unsafe.As<ulong, byte>(ref last);
    ref byte dst = ref bytes[^remaining];

#pragma warning disable S907 // "goto" statement should not be used
    switch (remaining) {
      case 7: Unsafe.Add(ref dst, 6) = Unsafe.Add(ref src, 6); goto case 6;
      case 6: Unsafe.Add(ref dst, 5) = Unsafe.Add(ref src, 5); goto case 5;
      case 5: Unsafe.Add(ref dst, 4) = Unsafe.Add(ref src, 4); goto case 4;
      case 4: Unsafe.Add(ref dst, 3) = Unsafe.Add(ref src, 3); goto case 3;
      case 3: Unsafe.Add(ref dst, 2) = Unsafe.Add(ref src, 2); goto case 2;
      case 2: Unsafe.Add(ref dst, 1) = Unsafe.Add(ref src, 1); goto case 1;
      case 1: dst = src; break;
    }
#pragma warning restore S907 // "goto" statement should not be used
  }

  #endregion

  #region T where T : class, IRandom

  /// <summary>
  /// Returns a random 64-bit unsigned integer in the range
  /// [0, <paramref name="max"/>).
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <param name="max">
  /// The exclusive upper bound. Must be greater than 0.
  /// </param>
  /// <returns> A random <see cref="ulong"/> by Generated. </returns>
  /// <remarks>
  /// <para>
  /// The method uses the unbiased rejection sampling technique to avoid
  /// modulo bias.
  /// It repeatedly generates random values until one falls below the
  /// rejection threshold,
  /// then returns the remainder modulo <paramref name="max"/>.
  /// </para>
  /// <para>
  /// If <paramref name="max"/> is 0, the method returns 0 without
  /// generating any random numbers.
  /// </para>
  /// </remarks>
  static virtual ulong NextUInt64<T>(T rand, ulong max)
    where T : class, IRandom {
    if (max is 0) return 0;
    ulong threshold = unchecked((0ul - max) % max);
    while (true) {
      ulong r = rand.NextUInt64();
      if (r >= threshold) return r % max;
    }
  }

  /// <summary>
  /// Returns a random 64-bit unsigned integer in the range
  /// [<paramref name="min"/>, <paramref name="max"/>).
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <param name="min">The inclusive lower bound.</param>
  /// <param name="max">
  /// The exclusive upper bound. Must be greater than <paramref name="min"/>.
  /// </param>
  /// <returns> A random <see cref="ulong"/> by Generated.</returns>
  /// <remarks>
  /// If <paramref name="max"/> is less than <paramref name="min"/>,
  /// the method returns 0 without generating random  numbers.
  /// </remarks>
  static virtual ulong NextUInt64<T>(T rand, ulong min, ulong max)
    where T : class, IRandom {
    if (min >= max) return 0;
    var range = max - min;
    return min + rand.NextUInt64(range);
  }

  /// <summary>
  /// Returns a random <see cref="double"/> in the range [0, 1).
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <returns>A double in [0, 1).</returns>
  /// <remarks>
  /// The value is generated by shifting the 64-bit random value right by
  /// <see cref="DoubleShift"/> bits to obtain a 53-bit integer,
  /// then multiplying by <see cref="DoubleScale"/>.
  /// This yields a uniform distribution with 53 bits of precision.
  /// </remarks>
  static virtual double NextDouble<T>(T rand) where T : class, IRandom
    => (rand.NextUInt64() >> DoubleShift) * DoubleScale;

  /// <summary>
  /// Returns a random <see cref="double"/> in the range [0, 1].
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <returns>A double in [0, 1], inclusive of 1.</returns>
  /// <remarks>
  /// The value is obtained by dividing the full 64-bit random integer by
  /// <see cref="ulong.MaxValue"/>. This gives a uniform distribution over
  /// all possible <see cref="double"/> values in that range, though the
  /// granularity is limited by the representation of <see cref="double"/>.
  /// </remarks>
  static virtual double NextDoubleInclusive<T>(T rand)
    where T : class, IRandom
    => rand.NextUInt64() / (double)ulong.MaxValue;

  /// <summary>
  /// Fills the elements of a <see cref="Span{T}"/> with random
  /// <see cref="ulong"/> values.
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <param name="buffer">
  /// The span to fill. If empty, the method returns immediately.
  /// </param>
  static virtual void Fill<T>(T rand, scoped Span<ulong> buffer)
    where T : class, IRandom {
    if (buffer.IsEmpty) return;
    foreach (ref var value in buffer)
      value = rand.NextUInt64();
  }

  /// <summary>
  /// Fills the elements of a <see cref="Span{T}"/> with random values of
  /// any <see langword="unmanaged"/> type.
  /// </summary>
  /// <typeparam name="T">The random number generators type.</typeparam>
  /// <typeparam name="U">The unmanaged element type.</typeparam>
  /// <param name="rand">The random number generator.</param>
  /// <param name="buffer">
  /// The span to fill. If empty, the method returns immediately.
  /// </param>
  /// <remarks>
  /// The method converts the span to a byte sequence,
  /// fills complete 8-byte chunks as <see cref="ulong"/> values,
  /// then copies any remaining bytes from an extra random value
  /// using a <c>switch</c> with fallthrough <c>goto case</c> statements.
  /// </remarks>
  static virtual void Fill<T, U>(T rand, scoped Span<U> buffer)
    where T : class, IRandom
    where U : unmanaged {
    if (buffer.IsEmpty) return;

    var bytes = MemoryMarshal.AsBytes(buffer);
    var sp = MemoryMarshal.Cast<byte, ulong>(bytes);
    rand.Fill(sp);

    int remaining = bytes.Length % 8;
    if (remaining is 0) return;

    ulong last = rand.NextUInt64();
    ref byte src = ref Unsafe.As<ulong, byte>(ref last);
    ref byte dst = ref bytes[^remaining];

#pragma warning disable S907 // "goto" statement should not be used
    switch (remaining) {
      case 7: Unsafe.Add(ref dst, 6) = Unsafe.Add(ref src, 6); goto case 6;
      case 6: Unsafe.Add(ref dst, 5) = Unsafe.Add(ref src, 5); goto case 5;
      case 5: Unsafe.Add(ref dst, 4) = Unsafe.Add(ref src, 4); goto case 4;
      case 4: Unsafe.Add(ref dst, 3) = Unsafe.Add(ref src, 3); goto case 3;
      case 3: Unsafe.Add(ref dst, 2) = Unsafe.Add(ref src, 2); goto case 2;
      case 2: Unsafe.Add(ref dst, 1) = Unsafe.Add(ref src, 1); goto case 1;
      case 1: dst = src; break;
    }
#pragma warning restore S907 // "goto" statement should not be used
  }
  #endregion
}
#pragma warning restore S4136 // Method overloads should be grouped together
