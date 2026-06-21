
using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace Utils;

public static class Numeric {
  public const uint KiB = 1024;
  public const uint MiB = KiB * KiB;
  public const uint GiB = MiB * KiB;

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
}
