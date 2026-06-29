
using System.Runtime.CompilerServices;

namespace Core.Random;

public struct SplitMix64(ulong x) {
  public const ulong GoldenRatio = 0x9E3779B97F4A7C15;
  public const ulong MixConst1 = 0xBF58476D1CE4E5B9;
  public const ulong MixConst2 = 0x94D049BB133111EB;

  public const int RotateS0 = 24;
  public const int RotateS1 = 37;
  public const int RotateS3 = 45;
  public const int RotateS7 = 29;
  public const int ShiftS1 = 16;

  public const int DoubleShift = 11;
  public const int DoublePrecision = 53;
  public const double DoubleScale = 1.0 / (1UL << DoublePrecision);

  private ulong _state = x;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    ulong z = this._state += GoldenRatio;
    z = (z ^ (z >> 30)) * MixConst1;
    z = (z ^ (z >> 27)) * MixConst2;
    return z ^ (z >> 31);
  }

  public ulong NextUInt64(ulong max) {
    if (max == 0) return 0;
    ulong threshold = unchecked((0ul - max) % max);
    while (true) {
      ulong r = this.NextUInt64();
      if (r >= threshold) return r % max;
    }
  }

  public long NextInt64(long min, long max) {
    if (min > max) return 0;
    ulong range = (ulong)(max - min);
    return min + (long)this.NextUInt64(range + 1);
  }

  public double NextDouble()
    => (this.NextUInt64() >> DoubleShift) * DoubleScale;

  public double NextDoubleInclusive()
    => this.NextUInt64() / (double)ulong.MaxValue;
}
