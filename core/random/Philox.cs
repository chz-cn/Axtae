
using System.Runtime.CompilerServices;
using static Core.Numeric;

namespace Core.Random;

public sealed class Philox4x32 : IRandom {
  public const uint Round0 = 0xD2511F53;
  public const uint Round1 = 0xCD9E8D57;
  public const uint Round2 = 0x9E3779B9;
  public const uint Round3 = 0x9D9C4F0F;

  private uint _ctr0, _ctr1, _ctr2, _ctr3;
  private readonly uint _key0, _key1;

#pragma warning disable S1144 // Unused private types or members should be removed
  [InlineArray(4)]
  private struct Buffer { public uint V; }
#pragma warning restore S1144 // Unused private types or members should be removed

  private Buffer _buffer = new();

  private int _index = 4;

  public Philox4x32(ulong seed) {
    ulong key64 = new SplitMix64(seed).NextUInt64();

    this._key0 = (uint)key64;
    this._key1 = (uint)(key64 >> 32);

    this._ctr0 = this._ctr1 = this._ctr2 = this._ctr3 = 0;
  }

  public Philox4x32(uint c0, uint c1, uint c2, uint c3, uint k0, uint k1) {
    (this._ctr0, this._ctr1, this._ctr2, this._ctr3) = (c0, c1, c2, c3);
    (this._key0, this._key1) = (k0, k1);
  }

  public uint NextUInt32() {
    if (this._index >= 4) {
      this.FillBuffer();
      this._index = 0;
    }
    return this._buffer[this._index++];
  }

  public ulong NextUInt64() {
    uint low = this.NextUInt32();
    uint high = this.NextUInt32();
    return ((ulong)high << 32) | low;
  }

  public void JumpTo(uint c0, uint c1, uint c2, uint c3) {
    (this._ctr0, this._ctr1, this._ctr2, this._ctr3) = (c0, c1, c2, c3);
    this._index = 4;
  }

  private void FillBuffer() {
    var (c0, c1, c2, c3) = (this._ctr0, this._ctr1, this._ctr2, this._ctr3);
    var (k0, k1) = (this._key0, this._key1);

    for (nuint round = 0; round < 10; round++) {
      uint r0 = Round0 + k0;
      uint r1 = Round1 + k1;
      uint r2 = Round2 + k0;
      uint r3 = Round3 + k1;

      uint x0 = MulHi(c0, Round0) + c1;
      uint x1 = MulHi(c1, Round1) + c2;
      uint x2 = MulHi(c2, Round2) + c3;
      uint x3 = MulHi(c3, Round3) + c0;

      x0 ^= r0;
      x1 ^= r1;
      x2 ^= r2;
      x3 ^= r3;

      (c0, c1, c2, c3) = (x0, x1, x2, x3);

      k0 += Round2;
      k1 += Round2;
    }

    this._buffer[0] = c0;
    this._buffer[1] = c1;
    this._buffer[2] = c2;
    this._buffer[3] = c3;

    this._ctr0++;
    if (this._ctr0 is 0) {
      this._ctr1++;
      if (this._ctr1 is 0) {
        this._ctr2++;
        if (this._ctr2 is 0)
          this._ctr3++;
      }
    }
  }
}

public sealed class Philox4x64 : IRandom {
  public const ulong Round0 = 0xD2E7470EE14C6C93;
  public const ulong Round1 = 0xCA5A8263951AF3E3;
  public const ulong Round2 = 0x9E3779B97F4A7C15;
  public const ulong Round3 = 0x8F98C623BACD3F9F;

  private ulong _ctr0, _ctr1, _ctr2, _ctr3;
  private readonly ulong _key0, _key1;

#pragma warning disable S1144 // Unused private types or members should be removed
  [InlineArray(4)]
  private struct Buffer { public ulong V; }
#pragma warning restore S1144 // Unused private types or members should be removed

  private Buffer _buffer = new();

  private int _index = 4;

  public Philox4x64(ulong seed) {
    SplitMix64 mix = new(seed);
    this._key0 = mix.NextUInt64();
    this._key1 = mix.NextUInt64();

    this._ctr0 = this._ctr1 = this._ctr2 = this._ctr3 = 0;
  }

  public Philox4x64(ulong c0, ulong c1, ulong c2, ulong c3,
    ulong k0, ulong k1) {
    (this._ctr0, this._ctr1, this._ctr2, this._ctr3) = (c0, c1, c2, c3);
    (this._key0, this._key1) = (k0, k1);
  }

  public ulong NextUInt64() {
    if (this._index >= 4) {
      this.FillBuffer();
      this._index = 0;
    }
    return this._buffer[this._index++];
  }

  public void JumpTo(ulong c0, ulong c1, ulong c2, ulong c3) {
    (this._ctr0, this._ctr1, this._ctr2, this._ctr3) = (c0, c1, c2, c3);
    this._index = 4;
  }

  private void FillBuffer() {
    var (c0, c1, c2, c3) = (this._ctr0, this._ctr1, this._ctr2, this._ctr3);
    var (k0, k1) = (this._key0, this._key1);

    for (nuint round = 0; round < 10; round++) {
      ulong r0 = Round0 + k0;
      ulong r1 = Round1 + k1;
      ulong r2 = Round2 + k0;
      ulong r3 = Round3 + k1;

      ulong x0 = MulHi(c0, Round0) + c1;
      ulong x1 = MulHi(c1, Round1) + c2;
      ulong x2 = MulHi(c2, Round2) + c3;
      ulong x3 = MulHi(c3, Round3) + c0;

      x0 ^= r0;
      x1 ^= r1;
      x2 ^= r2;
      x3 ^= r3;

      (c0, c1, c2, c3) = (x0, x1, x2, x3);

      k0 += Round2;
      k1 += Round2;
    }

    this._buffer[0] = c0;
    this._buffer[1] = c1;
    this._buffer[2] = c2;
    this._buffer[3] = c3;

    this._ctr0++;
    if (this._ctr0 is 0) {
      this._ctr1++;
      if (this._ctr1 is 0) {
        this._ctr2++;
        if (this._ctr2 is 0)
          this._ctr3++;
      }
    }
  }
}
