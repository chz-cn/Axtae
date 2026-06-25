
namespace Core;

public static class Per50000 {
  public const ushort One = 50000;
  public const uint OneSquared = (uint)One * One;

  public static uint CalcDI(uint damage, ushort DI) {
    ulong tmp = (ulong)damage
      * (uint)(One - DI) / One;
    return (uint)tmp;
  }

  public static uint CalcDIV(uint damage, ushort DR, ushort Vul) {
    ulong tmp = (ulong)damage
      * (uint)(One - DR) / One
      * (uint)(One + Vul) / One;
    return (uint)tmp;
  }

  public static uint CalcDamage(uint damage, ushort DI, ushort DR, ushort Vul) {
    ulong tmp = (ulong)damage
      * (uint)(One - DI) / One
      * (uint)(One - DR) / One
      * (uint)(One + Vul) / One;
    return (uint)tmp;
  }
}
