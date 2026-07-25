
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Axtae;

/// <summary>
/// Provides numeric constants and utility methods for common operations.
/// </summary>
public static class Numeric {
  /// <summary>
  /// Represents one kibibyte (1024 bytes).
  /// </summary>
  public const uint KiB = 1024;

  /// <summary>
  /// Represents one mebibyte (1024 * 1024 bytes).
  /// </summary>
  public const uint MiB = KiB * KiB;

  /// <summary>
  /// Represents one gibibyte (1024 * 1024 * 1024 bytes).
  /// </summary>
  public const uint GiB = MiB * KiB;

  /// <summary>
  /// Sets elements of the span to zero if they are less than the specified threshold.
  /// </summary>
  /// <param name="data">The span of float values to process.</param>
  /// <param name="threshold">The threshold value. Elements less than this will be set to zero.</param>
  /// <remarks>
  /// Uses vectorized SIMD operations for performance when possible,
  /// and falls back to a scalar loop for remaining elements.
  /// </remarks>
  public static void ZeroIfLessThan(Span<float> data, float threshold) {
    if (data.IsEmpty) return;

    int vec_size = Vector<float>.Count;

    Vector<float> right = new(threshold);
    Vector<float> zero = Vector<float>.Zero;

    ref float start = ref data[0];
    int i = 0;
    int last_vec_start = data.Length - vec_size;

    while (i <= last_vec_start) {
      Vector<float> vec = Vector.LoadUnsafe(ref start, (nuint)i);
      Vector<int> mask = Vector.LessThan(vec, right);

      Vector.ConditionalSelect(mask, zero, vec)
        .StoreUnsafe(ref start, (nuint)i);
      i += vec_size;
    }

    int len = data.Length;
    while (i < len) {
      if (data[i] < threshold)
        data[i] = 0;
      i++;
    }
  }

  /// <summary>
  /// Sets elements of the span to zero if they are less than the specified threshold,
  /// assuming the data is aligned for vectorized processing.
  /// </summary>
  /// <param name="data">The span of float values to process.</param>
  /// <param name="threshold">The threshold value. Elements less than this will be set to zero.</param>
  /// <remarks>
  /// This method uses only vectorized SIMD operations and does not include a scalar fallback loop.
  /// The caller should ensure the data length is suitable for vectorized processing.
  /// </remarks>
  public static void ZeroIfLessThanAligned(Span<float> data, float threshold) {
    int vec_size = Vector<float>.Count;
    Vector<float> right = new(threshold);
    Vector<float> zero = Vector<float>.Zero;

    ref float start = ref data[0];
    int len = data.Length;
    for (int i = 0; i < len; i += vec_size) {
      Vector<float> vec = Vector.LoadUnsafe(ref start, (nuint)i);
      Vector<int> mask = Vector.LessThan(vec, right);
      Vector.ConditionalSelect(mask, zero, vec)
        .StoreUnsafe(ref start, (nuint)i);
    }
  }

  /// <summary>
  /// Computes the high 32 bits of the 64-bit product of two 32-bit unsigned integers.
  /// </summary>
  /// <param name="a">The first 32-bit unsigned integer.</param>
  /// <param name="b">The second 32-bit unsigned integer.</param>
  /// <returns>The high 32 bits of the 64-bit product.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static uint MulHi(uint a, uint b) => (uint)(Math.BigMul(a, b) >> 32);

  /// <summary>
  /// Computes the high 64 bits of the 128-bit product of two 64-bit unsigned integers.
  /// </summary>
  /// <param name="a">The first 64-bit unsigned integer.</param>
  /// <param name="b">The second 64-bit unsigned integer.</param>
  /// <returns>The high 64 bits of the 128-bit product.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ulong MulHi(ulong a, ulong b) => Math.BigMul(a, b, out _);
}
