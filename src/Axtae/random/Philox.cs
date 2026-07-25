
using System.Runtime.CompilerServices;
using static Axtae.Numeric;

namespace Axtae.Random;

/// <summary>
/// A Philox 4x32-bit pseudo-random number generator
/// (based on the Philox algorithm).
/// </summary>
/// <remarks>
/// <para>
/// Philox is a counter-based PRNG that uses a cryptographic permutation
/// function.
/// It is particularly well-suited for parallel applications because it has no
/// mutable state other than the counter and key.
/// </para>
/// <para>
/// This implementation uses 10 rounds and produces 32-bit values internally,
/// combining them to form 64-bit outputs.
/// </para>
/// </remarks>
public sealed class Philox4x32 : IRandom {
  /// <summary>Round constant 0 for the Philox4x32 permutation.</summary>
  public const uint Round0 = 0xD2511F53;
  /// <summary>Round constant 1 for the Philox4x32 permutation.</summary>
  public const uint Round1 = 0xCD9E8D57;
  /// <summary>Round constant 2 for the Philox4x32 permutation.</summary>
  public const uint Round2 = 0x9E3779B9;
  /// <summary>Round constant 3 for the Philox4x32 permutation.</summary>
  public const uint Round3 = 0x9D9C4F0F;

  private uint _ctr0, _ctr1, _ctr2, _ctr3;
  private readonly uint _key0, _key1;

  private InlineArray4<uint> _buffer = new();

  private int _index = 4;

  /// <summary>
  /// Initializes a new <see cref="Philox4x32"/> instance with the specified seed.
  /// </summary>
  /// <param name="seed">
  /// The seed value used to derive the key via SplitMix64.
  /// </param>
  public Philox4x32(ulong seed) {
    ulong key64 = SplitMix64.Mix(seed);

    this._key0 = (uint)key64;
    this._key1 = (uint)(key64 >> 32);

    this._ctr0 = this._ctr1 = this._ctr2 = this._ctr3 = 0;
  }

  /// <summary>
  /// Initializes a new <see cref="Philox4x32"/> instance with explicit
  /// counter and key values.
  /// </summary>
  /// <param name="c0">Counter value 0.</param>
  /// <param name="c1">Counter value 1.</param>
  /// <param name="c2">Counter value 2.</param>
  /// <param name="c3">Counter value 3.</param>
  /// <param name="k0">Key value 0.</param>
  /// <param name="k1">Key value 1.</param>
  public Philox4x32(uint c0, uint c1, uint c2, uint c3, uint k0, uint k1) {
    (this._ctr0, this._ctr1, this._ctr2, this._ctr3) = (c0, c1, c2, c3);
    (this._key0, this._key1) = (k0, k1);
  }

  /// <summary>
  /// Generates the next 32-bit unsigned integer from the generator.
  /// </summary>
  /// <returns>A 32-bit unsigned random integer.</returns>
  public uint NextUInt32() {
    if (this._index >= 4) {
      this.FillBuffer();
      this._index = 0;
    }
    return this._buffer[this._index++];
  }

  /// <inheritdoc/>
  public ulong NextUInt64() {
    uint low = this.NextUInt32();
    uint high = this.NextUInt32();
    return ((ulong)high << 32) | low;
  }

  /// <summary>
  /// Jumps the generator to the specified counter position.
  /// </summary>
  /// <param name="c0">New counter value 0.</param>
  /// <param name="c1">New counter value 1.</param>
  /// <param name="c2">New counter value 2.</param>
  /// <param name="c3">New counter value 3.</param>
  /// <remarks>
  /// This allows deterministic positioning within the Philox stream.
  /// The internal buffer is invalidated and will be refilled on the next read.
  /// </remarks>
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

/// <summary>
/// A Philox 4x64-bit pseudo-random number generator
/// (based on the Philox algorithm).
/// </summary>
/// <remarks>
/// <para>
/// Philox is a counter-based PRNG that uses a cryptographic permutation
/// function.
/// </para>
/// <para>
/// This implementation uses 10 rounds and produces 64-bit values directly.
/// </para>
/// </remarks>
public sealed class Philox4x64 : IRandom {
  /// <summary>Round constant 0 for the Philox4x64 permutation.</summary>
  public const ulong Round0 = 0xD2E7470EE14C6C93;
  /// <summary>Round constant 1 for the Philox4x64 permutation.</summary>
  public const ulong Round1 = 0xCA5A8263951AF3E3;
  /// <summary>Round constant 2 for the Philox4x64 permutation.</summary>
  public const ulong Round2 = 0x9E3779B97F4A7C15;
  /// <summary>Round constant 3 for the Philox4x64 permutation.</summary>
  public const ulong Round3 = 0x8F98C623BACD3F9F;

  private ulong _ctr0, _ctr1, _ctr2, _ctr3;
  private readonly ulong _key0, _key1;

  private InlineArray4<ulong> _buffer = new();

  private int _index = 4;

  /// <summary>
  /// Initializes a new <see cref="Philox4x64"/> instance with the specified seed.
  /// </summary>
  /// <param name="seed">The seed value used to derive the keys via SplitMix64.</param>
  public Philox4x64(ulong seed) {
    SplitMix64 mix = new(seed);
    this._key0 = mix.NextUInt64();
    this._key1 = mix.NextUInt64();

    this._ctr0 = this._ctr1 = this._ctr2 = this._ctr3 = 0;
  }

  /// <summary>
  /// Initializes a new <see cref="Philox4x64"/> instance with explicit counter and key values.
  /// </summary>
  /// <param name="c0">Counter value 0.</param>
  /// <param name="c1">Counter value 1.</param>
  /// <param name="c2">Counter value 2.</param>
  /// <param name="c3">Counter value 3.</param>
  /// <param name="k0">Key value 0.</param>
  /// <param name="k1">Key value 1.</param>
  public Philox4x64(ulong c0, ulong c1, ulong c2, ulong c3,
    ulong k0, ulong k1) {
    (this._ctr0, this._ctr1, this._ctr2, this._ctr3) = (c0, c1, c2, c3);
    (this._key0, this._key1) = (k0, k1);
  }

  /// <inheritdoc/>
  public ulong NextUInt64() {
    if (this._index >= 4) {
      this.FillBuffer();
      this._index = 0;
    }
    return this._buffer[this._index++];
  }

  /// <summary>
  /// Jumps the generator to the specified counter position.
  /// </summary>
  /// <param name="c0">New counter value 0.</param>
  /// <param name="c1">New counter value 1.</param>
  /// <param name="c2">New counter value 2.</param>
  /// <param name="c3">New counter value 3.</param>
  /// <remarks>
  /// This allows deterministic positioning within the Philox stream.
  /// The internal buffer is invalidated and will be refilled on the next read.
  /// </remarks>
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
