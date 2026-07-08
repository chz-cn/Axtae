
using static Core.Random.IRandom;

namespace Core.Random;

public interface IRandom {
  const ulong GoldenRatio = 0x9E3779B97F4A7C15;
  const ulong MixConst1 = 0xBF58476D1CE4E5B9;
  const ulong MixConst2 = 0x94D049BB133111EB;

  const int RotateS0 = 24;
  const int RotateS1 = 37;
  const int RotateS3 = 45;
  const int RotateS7 = 29;
  const int ShiftS1 = 16;

  const int DoubleShift = 11;
  const int DoublePrecision = 53;
  const double DoubleScale = 1.0 / (1UL << DoublePrecision);

  ulong NextUInt64();
}

public static class IRandomExtensions {
  extension<T>(ref T random) where T : struct, IRandom, allows ref struct {
    public ulong NextUInt64(ulong max) {
      if (max is 0) return 0;
      ulong threshold = unchecked((0ul - max) % max);
      while (true) {
        ulong r = random.NextUInt64();
        if (r >= threshold) return r % max;
      }
    }

    public ulong NextUInt64(ulong min, ulong max) {
      if (min >= max) return 0;
      var range = max - min;
      return min + random.NextUInt64(range);
    }

    public double NextDouble()
      => (random.NextUInt64() >> DoubleShift) * DoubleScale;

    public double NextDoubleInclusive()
      => random.NextUInt64() / (double)ulong.MaxValue;
  }

  extension<T>(T random) where T : class, IRandom {
    public ulong NextUInt64(ulong max) {
      if (max is 0) return 0;
      ulong threshold = unchecked((0ul - max) % max);
      while (true) {
        ulong r = random.NextUInt64();
        if (r >= threshold) return r % max;
      }
    }

    public ulong NextUInt64(ulong min, ulong max) {
      if (min >= max) return 0;
      var range = max - min;
      return min + random.NextUInt64(range);
    }

    public double NextDouble()
      => (random.NextUInt64() >> DoubleShift) * DoubleScale;

    public double NextDoubleInclusive()
      => random.NextUInt64() / (double)ulong.MaxValue;
  }
}
