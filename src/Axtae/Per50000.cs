
namespace Axtae;

/// <summary>
/// Provides damage calculation utilities using a 1/50000 fixed-point
/// precision system.
/// </summary>
/// <remarks>
/// Damage reduction and vulnerability values are represented as integers in
/// the range [0, 50000],
/// where 50000 represents 100% (or 1.0 in floating-point).
/// </remarks>
public static class Per50000 {
  /// <summary>
  /// Represents the value 1.0 in the 1/50000 fixed-point system (50000).
  /// </summary>
  public const ushort One = 50000;

  /// <summary>
  /// Represents the square of <see cref="One"/> for intermediate calculations.
  /// </summary>
  public const uint OneSquared = (uint)One * One;

  /// <summary>
  /// Calculates damage after applying a damage immunity (DI) factor.
  /// </summary>
  /// <param name="damage">The base damage value.</param>
  /// <param name="DI">
  /// The damage immunity factor in 1/50000 fixed-point format.
  /// </param>
  /// <returns>The reduced damage value.</returns>
  public static uint CalcDI(uint damage, ushort DI) {
    ulong tmp = (ulong)damage
      * (uint)(One - DI) / One;
    return (uint)tmp;
  }

  /// <summary>
  /// Calculates damage after applying both damage reduction (DR) and
  /// vulnerability (Vul) factors.
  /// </summary>
  /// <param name="damage">The base damage value.</param>
  /// <param name="DR">
  /// The damage reduction factor in 1/50000 fixed-point format.
  /// </param>
  /// <param name="Vul">
  /// The vulnerability factor in 1/50000 fixed-point format.
  /// </param>
  /// <returns>
  /// The calculated damage value after reduction and vulnerability
  /// adjustments.
  /// </returns>
  public static uint CalcDIV(uint damage, ushort DR, ushort Vul) {
    ulong tmp = (ulong)damage
      * (uint)(One - DR) / One
      * (uint)(One + Vul) / One;
    return (uint)tmp;
  }

  /// <summary>
  /// Calculates damage after applying damage immunity (DI), damage reduction (DR),
  /// and vulnerability (Vul) factors.
  /// </summary>
  /// <param name="damage">The base damage value.</param>
  /// <param name="DI">The damage immunity factor in 1/50000 fixed-point format.</param>
  /// <param name="DR">The damage reduction factor in 1/50000 fixed-point format.</param>
  /// <param name="Vul">The vulnerability factor in 1/50000 fixed-point format.</param>
  /// <returns>The final calculated damage value.</returns>
  public static uint CalcDamage(uint damage, ushort DI, ushort DR, ushort Vul) {
    ulong tmp = (ulong)damage
      * (uint)(One - DI) / One
      * (uint)(One - DR) / One
      * (uint)(One + Vul) / One;
    return (uint)tmp;
  }
}
