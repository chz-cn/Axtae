
using System;
using System.Runtime.InteropServices;
using System.Threading;

using static Core.Numeric;

namespace Core;

public interface IPool {
  /// <summary>
  /// Gets the total size of the managed buffer in bytes.
  /// </summary>
  /// <value>The total number of bytes in the buffer.</value>
  public uint TotalByte { get; }

  /// <summary>
  /// Gets the size of each memory block in bytes.
  /// </summary>
  /// <value>The block size, which is a multiple of 64 bytes.</value>
  public uint BlockSize { get; }

  /// <summary>
  /// Gets the total number of blocks available in the pool.
  /// </summary>
  /// <value>The block count, which is at least 2.</value>
  public uint BlockCount { get; }

  unsafe byte* Alloc();
  unsafe void Free(byte* ptr);

  IOwner Rent();
}

/// <summary>
/// Represents a rented block of memory from a <see cref="PagePool"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a <see langword="readonly"/> <see langword="struct"/> that holds
/// a pointer to a memory block and its size. It is designed to be lightweight
/// and allocation-free.
/// </para>
/// <para>
/// The struct can be copied freely; however, <see cref="Dispose"/> should
/// only be called once per allocated block. Calling <see cref="Dispose"/>
/// on a copy does not invalidate other copies, so callers must manage
/// ownership carefully.
/// </para>
/// <para>
/// For safe bounds-checked access, use the <see cref="Span"/> property
/// instead of the indexer <see cref="this[uint]"/>.
/// </para>
/// </remarks>
public readonly unsafe struct IOwner(IPool parent, byte* ptr, uint size) {
  private readonly IPool _parent = parent;
  public readonly byte* Ptr = ptr;
  public readonly uint Size = size;

  /// <summary>
  /// Gets a value indicating whether this instance is empty.
  /// </summary>
  /// <value>
  /// <see langword="true"/> if the pointer is <see langword="null"/> or
  /// the size is zero otherwise, <see langword="false"/>.
  /// </value>
  public bool IsEmpty => this.Ptr is null || this.Size is 0;

  /// <summary>
  /// Gets a <see cref="Span{T}"/> over the rented memory block.
  /// </summary>
  /// <value>A <see cref="Span{byte}"/> representing the entire block.</value>
  /// <remarks>
  /// This property provides bounds-checked access and is the recommended way
  /// to read or write the rented memory.
  /// </remarks>
  public Span<byte> Span => new(this.Ptr, (int)this.Size);

  /// <summary>
  /// <paramref name="index"/> access is unchecked — use <see cref="Span"/>
  /// for safety.
  /// </summary>
  public byte this[nuint index] {
    get => this.Ptr[index];
    set => this.Ptr[index] = value;
  }

  public void Dispose() => this._parent?.Free(this.Ptr);
}

public unsafe sealed class PagePool : IDisposable, IPool {
  public uint TotalByte { get; }
  public uint BlockSize { get; }

  public uint BlockCount { get; }

  private uint _top = 0;
  private readonly ushort* _items;
  private readonly byte* _pool;

  private readonly Lock _lock = new();
  private bool _disposed = false;

  /// <summary>
  /// Initializes a new instance of the <see cref="PagePool"/> class.
  /// </summary>
  /// <param name="size">
  /// Total pool size in mebibytes (MiB). Must be between 1 and 2048.
  /// </param>
  /// <param name="block_size">
  /// Size of each page block in kibibytes (e.g., 1 = 4KiB, 2 = 8KiB).
  /// Must be greater than 0.
  /// </param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when:
  /// <list type="bullet">
  /// <item><paramref name="size"/> is 0 or greater than 2048.</item>
  /// <item><paramref name="block_size"/> is 0.</item>
  /// <item>
  /// The calculated block count is less than 2
  /// (pool too small for the given block size).
  /// </item>
  /// </list>
  /// </exception>
  /// <exception cref="OutOfMemoryException">
  /// Thrown when <see cref="NativeMemory.AlignedAlloc"/> returns
  /// <see langword="null"/>, indicating the system failed to allocate
  /// the requested memory.
  /// </exception>
  /// <remarks>
  /// <para>
  /// Powers of two are recommended for <paramref name="block_size"/> to
  /// enable optimization of division operations via bit shifting in hot
  /// paths (e.g., <see cref="Free"/>).
  /// </para>
  /// <para>
  /// The number of blocks is calculated as <c>floor(total_byte /
  /// (block_byte + 2))</c>, where <c>+2</c> accounts for one
  /// <see cref="ushort"/> index per block in metadata.
  /// Any remaining bytes are absorbed into the metadata section,
  /// ensuring zero wasted space.
  /// </para>
  /// <para>
  /// The entire pool is zero-initialized via <see cref="NativeMemory.Clear"/>
  /// during construction to pre-fault physical memory pages and avoid page
  /// faults in the hot path.
  /// </para>
  /// <para>
  /// During construction, all block indices are pushed onto the free stack
  /// using <see cref="TryPush"/>.
  /// This is a one-time initialization step and is not expected to fail.
  /// </para>
  /// </remarks>
  public PagePool(ushort size = 8, uint block_size = 1) {
    ArgumentOutOfRangeException.ThrowIfZero(size, nameof(size));
    ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 2048, nameof(size));

    ArgumentOutOfRangeException.ThrowIfZero(block_size, nameof(block_size));

    uint total_byte = size * MiB;
    uint block_byte = block_size * 4 * KiB;

    uint theoretical = total_byte / (block_byte + 2);
    ArgumentOutOfRangeException
      .ThrowIfLessThan(theoretical, 2u, nameof(theoretical));

    ushort block_count = (ushort)Math.Min(theoretical, ushort.MaxValue);

    byte* pool = (byte*)NativeMemory.AlignedAlloc(total_byte, 4 * KiB);

    this.TotalByte = total_byte;
    this.BlockSize = block_byte;
    this.BlockCount = block_count;

    nuint offset = block_count * block_byte;
    this._items = (ushort*)(pool + offset);
    this._pool = pool;

    NativeMemory.Clear(pool, total_byte);
    for (ushort i = 0; i < block_count; i++) {
      this.TryPush(i);
    }
  }

  /// <summary>
  /// Allocates a free data block from the pool.
  /// </summary>
  /// <returns>
  /// A pointer to the allocated block, or <see langword="null"/>
  /// if the pool is exhausted or disposed.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method is thread-safe and uses an internal lock.
  /// The returned pointer must be released with <see cref="Free"/>
  /// when no longer needed.
  /// </para>
  /// <para>
  /// The block is not zero-initialized; it contains whatever data
  /// was left from previous usage. Callers should overwrite the
  /// entire block before relying on its content.
  /// </para>
  /// </remarks>
  public byte* Alloc() {
    if (this._disposed) return null;
    lock (this._lock)
      if (!this._disposed && this.TryPop(out ushort index))
        return this._pool + index * this.BlockSize;

    return null;
  }

  /// <summary>
  /// Returns a previously allocated block to the pool.
  /// </summary>
  /// <param name="ptr">
  /// Pointer to the block, as returned by <see cref="Alloc"/>.
  /// </param>
  /// <remarks>
  /// <para>
  /// If <paramref name="ptr"/> is <see langword="null"/>, out of range,
  /// or not aligned to a block boundary, this method does nothing.
  /// </para>
  /// <para>
  /// This method is thread-safe. It acquires the internal lock only
  /// when the pointer is valid, so invalid calls are cheap.
  /// </para>
  /// <para>
  /// After calling <see cref="Dispose"/>, this method returns
  /// silently without freeing any memory.
  /// </para>
  /// </remarks>
  public void Free(byte* ptr) {
    byte* pool = this._pool;
    if (this._disposed || ptr < pool || ptr >= pool + this.TotalByte) return;

    uint offset = (uint)(ptr - pool);
    (uint index, uint r) = Math.DivRem(offset, this.BlockSize);

    if (r != 0 || index >= this.BlockCount) return;

    lock (this._lock)
      if (!this._disposed)
        this.TryPush((ushort)index);
  }

  /// <summary>
  /// Rents a memory block as a disposable <see cref="IOwner"/>.
  /// </summary>
  /// <returns>
  /// An <see cref="IOwner"/> instance representing the rented block,
  /// or <c>default</c> if no block is available.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method calls <see cref="Alloc"/> internally and wraps
  /// the result in an <see cref="IOwner"/>.
  /// </para>
  /// <para>
  /// The caller must call <see cref="IOwner.Dispose"/> on the
  /// returned instance to return the block to the pool.
  /// Failure to do so will leak the block.
  /// </para>
  /// <para>
  /// The returned <see cref="IOwner"/> is a readonly struct and
  /// can be copied, but only one copy should be disposed.
  /// </para>
  /// </remarks>
  public IOwner Rent() {
    byte* ptr = this.Alloc();
    if (ptr is null) return default;

    return new IOwner(this, ptr, this.BlockSize);
  }

  public void Dispose() {
    if (this._disposed) return;

    lock (this._lock) {
      this._disposed = true;
      NativeMemory.AlignedFree(this._pool);
    }
  }

  // stack
  private bool TryPush(ushort item) {
    if (this._top == this.BlockCount) return false;
    this._items[this._top] = item;
    this._top++;
    return true;
  }

  private bool TryPop(out ushort item) {
    if (this._top == 0) {
      item = default;
      return false;
    }
    this._top--;
    item = this._items[this._top];
    return true;
  }
}

/// <summary>
/// Manages memory blocks from a pre‑allocated external buffer using a bitmap.
/// </summary>
/// <remarks>
/// <para>
/// This pool does <em>not</em> allocate memory itself; instead, it operates
/// on a buffer provided by the caller. A bitmap stored at the end of the
/// buffer tracks which blocks are free (0) or allocated (1).
/// </para>
/// <para>
/// The total buffer size is specified in units of 4 KiB, and each block
/// size is given in units of 64 bytes. The block count is calculated such
/// that the bitmap fits exactly in the remaining space after all blocks.
/// The formula used is:
/// <c>block_count = floor((8 * total_byte - 7) / (8 * block_byte + 1))</c>.
/// This ensures the bitmap occupies the smallest possible number of bytes
/// while still addressing every block.
/// </para>
/// <para>
/// All public methods are thread‑safe and use an internal lock. The bitmap
/// is zero‑initialized at construction, indicating all blocks are free.
/// </para>
/// <para>
/// Because the buffer is owned externally, <see cref="Dispose"/> only marks
/// the pool as disposed and does <em>not</em> release the buffer memory.
/// </para>
/// </remarks>
public unsafe sealed class CachePool : IDisposable, IPool {
  public uint TotalByte { get; }
  public uint BlockSize { get; }

  public uint BlockCount { get; }

  private bool _disposed = false;

  private readonly ulong* _map;
  private readonly byte* _ptr;

  private readonly Lock _lock = new();

  /// <summary>
  /// Initializes a new instance of the <see cref="CachePool"/> class.
  /// </summary>
  /// <param name="ptr">
  /// Pointer to the start of a pre‑allocated buffer. The buffer must be
  /// large enough to hold both the data blocks and the bitmap.
  /// </param>
  /// <param name="size">
  /// Total buffer size in units of 4 KiB (i.e., <c>size * 4096</c> bytes).
  /// Must be greater than 0.
  /// </param>
  /// <param name="block_size">
  /// Size of each memory block in units of 64 bytes
  /// (i.e., <c>block_size * 64</c> bytes).
  /// Must be greater than 0.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="ptr"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when:
  /// <list type="bullet">
  /// <item>
  /// <paramref name="size"/> or <paramref name="block_size"/> is 0.
  /// </item>
  /// <item>
  /// The calculated block count is less than 2 (buffer too small for the
  /// given block size and bitmap overhead).
  /// </item>
  /// </list>
  /// </exception>
  /// <remarks>
  /// <para>
  /// The block count is derived from the total buffer size and the block size
  /// using the formula:
  /// <c>block_count = (8 * total_byte - 7) / (8 * block_byte + 1)</c>
  /// (integer division). This guarantees that the bitmap (one bit per block,
  /// rounded up to a multiple of 8 bytes) fits in the buffer after all data
  /// blocks.
  /// </para>
  /// <para>
  /// The bitmap is stored at the very end of the buffer and is cleared during
  /// construction. The buffer itself is not zeroed—only the bitmap is
  /// initialized.
  /// </para>
  /// </remarks>
  public CachePool(byte* ptr, ushort size, ushort block_size = 1) {
    ArgumentNullException.ThrowIfNull(ptr, nameof(ptr));

    ArgumentOutOfRangeException.ThrowIfZero(size, nameof(size));
    ArgumentOutOfRangeException.ThrowIfZero(block_size, nameof(block_size));

    uint total_byte = size * 4u * KiB;
    uint block_byte = block_size * 64u;


    uint block_count = (8 * total_byte - 7) / (8 * block_byte + 1);

    ArgumentOutOfRangeException
        .ThrowIfLessThan(block_count, 2u, nameof(block_count));

    this.TotalByte = total_byte;
    this.BlockSize = block_byte;
    this.BlockCount = block_count;

    nuint offset = block_count * block_byte;
    this._map = (ulong*)(ptr + offset);
    this._ptr = ptr;

    nuint byte_count = (block_count + 63) / 64 * 8;
    NativeMemory.Clear(this._map, byte_count);
  }

  /// <summary>
  /// Allocates a free data block from the pool.
  /// </summary>
  /// <returns>
  /// A pointer to the allocated block, or <see langword="null"/> if no
  /// block is available or the pool has been disposed.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method is thread‑safe. It scans the bitmap for the first free
  /// block, sets its bit to 1, and returns the corresponding pointer.
  /// </para>
  /// <para>
  /// The allocated block is <em>not</em> zero‑initialized; it may contain
  /// residual data from previous usage. Callers must overwrite the entire
  /// block before relying on its content.
  /// </para>
  /// </remarks>
  public byte* Alloc() {
    if (this._disposed) return null;
    uint count = (this.BlockCount + 63) / 64;

    lock (this._lock) {
      if (this._disposed) return null;

      for (uint idx = 0; idx < count; idx++) {
        ulong word = this._map[idx];

        if (word == ulong.MaxValue) continue;

        ulong inverted = ~word;
        int bit = System.Numerics.BitOperations.TrailingZeroCount(inverted);

        uint block_idx = (idx * 64) + (uint)bit;
        if (block_idx < this.BlockCount) {
          this._map[idx] |= 1ul << bit;
          return this._ptr + block_idx * this.BlockSize;
        }
      }
    }

    return null;
  }

  /// <summary>
  /// Returns a previously allocated block to the pool.
  /// </summary>
  /// <param name="ptr">
  /// Pointer to the block, as returned by <see cref="Alloc"/>.
  /// </param>
  /// <remarks>
  /// <para>
  /// If <paramref name="ptr"/> is <see langword="null"/>, out of the
  /// buffer range, not aligned to a block boundary, or its corresponding
  /// bit in the bitmap is already 0 (i.e., already free), this method
  /// does nothing.
  /// </para>
  /// <para>
  /// This method is thread‑safe. It acquires the internal lock only when
  /// the pointer is valid, so invalid calls are cheap.
  /// </para>
  /// <para>
  /// After the pool has been disposed, calls to this method return
  /// silently without modifying the bitmap.
  /// </para>
  /// </remarks>
  public void Free(byte* ptr) {
    if (this._disposed) return;

    byte* pool = this._ptr;
    if (ptr < pool || ptr >= pool + this.TotalByte) return;

    uint offset = (uint)(ptr - pool);
    uint index = offset / this.BlockSize;
    uint remainder = offset % this.BlockSize;

    if (remainder != 0 || index >= this.BlockCount) return;

    uint wordIdx = index / 64;
    uint bitOffset = index & 63;
    ulong mask = 1UL << (int)bitOffset;

    lock (this._lock) {
      if (this._disposed || (this._map[wordIdx] & mask) == 0) return;
      this._map[wordIdx] &= ~mask;
    }
  }

  /// <summary>
  /// Rents a memory block as a disposable <see cref="IOwner"/>.
  /// </summary>
  /// <returns>
  /// An <see cref="IOwner"/> instance representing the rented block,
  /// or <c>default</c> if no block is available.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method calls <see cref="Alloc"/> internally and wraps the
  /// result in an <see cref="IOwner"/>.
  /// </para>
  /// <para>
  /// The caller must call <see cref="IOwner.Dispose"/> on the returned
  /// instance to return the block to the pool. Failure to do so will
  /// leak the block.
  /// </para>
  /// <para>
  /// The returned <see cref="IOwner"/> is a readonly struct and can be
  /// copied, but only one copy should be disposed.
  /// </para>
  /// </remarks>
  public IOwner Rent() {
    byte* ptr = this.Alloc();
    if (ptr is null) return default;

    return new IOwner(this, ptr, this.BlockSize);
  }

  /// <summary>
  /// Disposes the pool, marking it as no longer usable.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This method is thread‑safe and sets the disposed flag. It does
  /// <em>not</em> free the external buffer because the pool does not own it.
  /// </para>
  /// <para>
  /// After disposal, all subsequent calls to <see cref="Alloc"/>,
  /// <see cref="Free"/>, and <see cref="Rent"/> will either return
  /// <see langword="null"/> or do nothing. It is safe to call this method
  /// multiple times.
  /// </para>
  /// </remarks>
  public void Dispose() {
    if (this._disposed) return;

    lock (this._lock) {
      this._disposed = true;
    }
  }
}
