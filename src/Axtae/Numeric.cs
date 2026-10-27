// Copyright (c) 2026 chz-cn
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.CompilerServices;

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
  /// Computes the full 64-bit product of two 32-bit unsigned integers,
  /// returning the high and low 32-bit halves.
  /// </summary>
  /// <param name="a">The first factor.</param>
  /// <param name="b">The second factor.</param>
  /// <returns>
  /// A tuple whose <c>High</c> element is bits 63..32 of the product and
  /// whose <c>Low</c> element is bits 31..0.
  /// </returns>
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public static (uint High, uint Low) Mul (uint a, uint b) {
    ulong v = Math.BigMul (a, b);
    return unchecked(((uint)(v >>> 32), (uint)v));
  }

  /// <summary>
  /// Computes the full 128-bit product of two 64-bit unsigned integers,
  /// returning the high and low 64-bit halves.
  /// </summary>
  /// <param name="a">The first factor.</param>
  /// <param name="b">The second factor.</param>
  /// <returns>
  /// A tuple whose <c>High</c> element is bits 127..64 of the product and
  /// whose <c>Low</c> element is bits 63..0.
  /// </returns>
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public static (ulong High, ulong Low) Mul (ulong a, ulong b) {
    ulong hi = Math.BigMul (a, b, out ulong lo);
    return (hi, lo);
  }
}
