
using System.Runtime.CompilerServices;
using static Core.Random.IRandom;

namespace Core.Random;

public struct SplitMix64(ulong x) : IRandom {
#pragma warning disable S3604 // Member initializer values should not be
  // redundant
  private ulong _state = x;
#pragma warning restore S3604 // Member initializer values should not be
  // redundant

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ulong NextUInt64() {
    ulong z = this._state += GoldenRatio;
    z = (z ^ (z >> 30)) * MixConst1;
    z = (z ^ (z >> 27)) * MixConst2;
    return z ^ (z >> 31);
  }

  public static ulong Mix(ref ulong state) {
    ulong z = state += GoldenRatio;
    z = (z ^ (z >> 30)) * MixConst1;
    z = (z ^ (z >> 27)) * MixConst2;
    return z ^ (z >> 31);
  }

  public static ulong Mix(ulong state) {
    ulong z = state + GoldenRatio;
    z = (z ^ (z >> 30)) * MixConst1;
    z = (z ^ (z >> 27)) * MixConst2;
    return z ^ (z >> 31);
  }
}
