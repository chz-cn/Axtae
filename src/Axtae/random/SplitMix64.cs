
using System.Runtime.CompilerServices;
using static Axtae.Random.IRandom;

namespace Axtae.Random;

public struct SplitMix64(ulong x) : IRandom {
  private ulong _state = x;

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
