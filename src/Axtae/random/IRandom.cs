
using static Axtae.Random.IRandom;

namespace Axtae.Random;

/// <summary>
/// Defines the contract for a pseudo‑random number generator that produces
/// 64‑bit unsigned integers.
/// </summary>
/// <remarks>
/// <para>
/// Implementations can be either structs (for deterministic, allocation‑free
/// usage) or classes (for shared, thread‑safe usage).
/// <b>To avoid boxing of <see langword="struct"/> implementations,
/// always use a generic constraint with <see langword="ref"/> T
/// <see langword="where"/> T : <see cref="IRandom"/></b>
/// when passing <see langword="struct"/> instances as parameters.
/// Extension methods in <see cref="IRandomExtensions"/> follow this pattern.
/// </para>
/// <para>
/// Implementations should provide a deterministic sequence of pseudo‑random
/// numbers based on an internal state.
/// The constants defined in this interface (e.g., <see cref="GoldenRatio"/>,
/// <see cref="MixConst1"/>, and various shift values) are intended for use in
/// common mixing functions, such as the SplitMix64 algorithm, and can be
/// reused by implementers.
/// </para>
/// </remarks>
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
  const int ShiftS1 = 16;

  /// <summary>
  /// Number of bits to shift right when converting a 64‑bit value to a
  /// <see cref="double"/> in the range [0, 1).
  /// </summary>
  const int DoubleShift = 11;

  /// <summary>
  /// Number of bits of precision used for <see cref="double"/> conversion.
  /// </summary>
  const int DoublePrecision = 53;

  /// <summary>
  /// Scaling factor to convert a 53‑bit integer to a <see cref="double"/>
  /// in [0, 1).
  /// </summary>
  const double DoubleScale = 1.0 / (1UL << DoublePrecision);

  /// <summary>
  /// Generates a uniformly distributed 64‑bit unsigned integer.
  /// </summary>
  /// <returns>A pseudo‑random <see cref="ulong"/> value.</returns>
  ulong NextUInt64();
}

/// <summary>
/// Provides extension methods for <see cref="IRandom"/> implementations.
/// </summary>
/// <remarks>
/// These methods are available for both <see langword="struct"/> and
/// <see langword="class"/> implementations of <see cref="IRandom"/> via the
/// two <see langword="extension"/> blocks.
/// The overloads are provided to avoid boxing of struct instances.
/// </remarks>
public static class IRandomExtensions {
  extension<T>(ref T random) where T : struct, IRandom, allows ref struct {
    /// <summary>
    /// Returns a random 64‑bit unsigned integer in the range
    /// [0, <paramref name="max"/>).
    /// </summary>
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
    public ulong NextUInt64(ulong max) {
      if (max is 0) return 0;
      ulong threshold = unchecked((0ul - max) % max);
      while (true) {
        ulong r = random.NextUInt64();
        if (r >= threshold) return r % max;
      }
    }

    /// <summary>
    /// Returns a random 64‑bit unsigned integer in the range
    /// [<paramref name="min"/>, <paramref name="max"/>).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">
    /// The exclusive upper bound. Must be greater than <paramref name="min"/>.
    /// </param>
    /// <returns> A random <see cref="ulong"/> by Generated.</returns>
    /// <remarks>
    /// If <paramref name="max"/> is less than <paramref name="min"/>,
    /// the method returns 0 without generating random  numbers.
    /// </remarks>
    public ulong NextUInt64(ulong min, ulong max) {
      if (min >= max) return 0;
      var range = max - min;
      return min + random.NextUInt64(range);
    }

    /// <summary>
    /// Returns a random <see cref="double"/> in the range [0, 1).
    /// </summary>
    /// <returns>A double in [0, 1).</returns>
    /// <remarks>
    /// The value is generated by shifting the 64‑bit random value right by
    /// <see cref="DoubleShift"/> bits to obtain a 53‑bit integer,
    /// then multiplying by <see cref="DoubleScale"/>.
    /// This yields a uniform distribution with 53 bits of precision.
    /// </remarks>
    public double NextDouble()
      => (random.NextUInt64() >> DoubleShift) * DoubleScale;

    /// <summary>
    /// Returns a random <see cref="double"/> in the range [0, 1].
    /// </summary>
    /// <returns>A double in [0, 1], inclusive of 1.</returns>
    /// <remarks>
    /// The value is obtained by dividing the full 64‑bit random integer by
    /// <see cref="ulong.MaxValue"/>. This gives a uniform distribution over
    /// all possible <see cref="double"/> values in that range, though the
    /// granularity is limited by the representation of <see cref="double"/>.
    /// </remarks>
    public double NextDoubleInclusive()
      => random.NextUInt64() / (double)ulong.MaxValue;
  }

  extension<T>(T random) where T : class, IRandom {
    /// <summary>
    /// Returns a random 64‑bit unsigned integer in the range
    /// [0, <paramref name="max"/>).
    /// </summary>
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
    public ulong NextUInt64(ulong max) {
      if (max is 0) return 0;
      ulong threshold = unchecked((0ul - max) % max);
      while (true) {
        ulong r = random.NextUInt64();
        if (r >= threshold) return r % max;
      }
    }

    /// <summary>
    /// Returns a random 64‑bit unsigned integer in the range
    /// [<paramref name="min"/>, <paramref name="max"/>).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">
    /// The exclusive upper bound. Must be greater than <paramref name="min"/>.
    /// </param>
    /// <returns> A random <see cref="ulong"/> by Generated.</returns>
    /// <remarks>
    /// If <paramref name="max"/> is less than <paramref name="min"/>,
    /// the method returns 0 without generating random  numbers.
    /// </remarks>
    public ulong NextUInt64(ulong min, ulong max) {
      if (min >= max) return 0;
      var range = max - min;
      return min + random.NextUInt64(range);
    }

    /// <summary>
    /// Returns a random <see cref="double"/> in the range [0, 1).
    /// </summary>
    /// <returns>A double in [0, 1).</returns>
    /// <remarks>
    /// The value is generated by shifting the 64‑bit random value right by
    /// <see cref="DoubleShift"/> bits to obtain a 53‑bit integer,
    /// then multiplying by <see cref="DoubleScale"/>.
    /// This yields a uniform distribution with 53 bits of precision.
    /// </remarks>
    public double NextDouble()
      => (random.NextUInt64() >> DoubleShift) * DoubleScale;

    /// <summary>
    /// Returns a random <see cref="double"/> in the range [0, 1].
    /// </summary>
    /// <returns>A double in [0, 1], inclusive of 1.</returns>
    /// <remarks>
    /// The value is obtained by dividing the full 64‑bit random integer by
    /// <see cref="ulong.MaxValue"/>. This gives a uniform distribution over
    /// all possible <see cref="double"/> values in that range, though the
    /// granularity is limited by the representation of <see cref="double"/>.
    /// </remarks>
    public double NextDoubleInclusive()
      => random.NextUInt64() / (double)ulong.MaxValue;
  }
}
