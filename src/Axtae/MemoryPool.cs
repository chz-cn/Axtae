
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using static Axtae.Numeric;

namespace Axtae;

/// <summary>
/// Defines a memory pool that manages fixed-size blocks of unmanaged memory.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface provide a mechanism for allocating and
/// reusing memory blocks of a uniform size (<see cref="BlockSize"/>). This
/// reduces allocation pressure and improves performance in scenarios with
/// frequent memory operations.
/// </para>
/// <para>
/// The pool is typically pre-allocated with a fixed number of blocks
/// (<see cref="BlockCount"/>), and the total capacity is
/// <see cref="TotalByte"/>.
/// </para>
/// <para>
/// All methods are thread-safe unless otherwise noted by the implementation.
/// </para>
/// </remarks>
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

#pragma warning disable S6640 // Unsafe code blocks should not be used

  /// <summary>
  /// Allocates a single memory block from the pool.
  /// </summary>
  /// <returns>
  /// A pointer to the allocated block, or <see langword="null"/> if no block
  /// is currently available.
  /// </returns>
  /// <remarks>
  /// <para>
  /// The returned pointer points to a block of size <see cref="BlockSize"/>
  /// bytes. The memory is not initialized; callers must ensure they write
  /// meaningful data before reading.
  /// </para>
  /// <para>
  /// If the pool is exhausted, this method may return <see langword="null"/>.
  /// Some implementations may block or throw an exception; check the
  /// specific implementation's documentation.
  /// </para>
  /// <para>
  /// The allocated block must be returned to the pool via <see cref="Free"/>
  /// when no longer needed to avoid memory leaks.
  /// </para>
  /// </remarks>
  unsafe byte* Alloc();

  /// <summary>
  /// Returns a previously allocated memory block to the pool.
  /// </summary>
  /// <param name="ptr">A pointer to the block to free. Must have been
  /// obtained from <see cref="Alloc"/> and not already freed.</param>
  /// <remarks>
  /// <para>
  /// After calling <see cref="Free"/>, the pointer is no longer valid and
  /// should not be dereferenced or passed to <see cref="Free"/> again.
  /// </para>
  /// <para>
  /// Passing a <see langword="null"/> pointer is a no-op in most
  /// implementations, but callers should avoid doing so unless
  /// documented.
  /// </para>
  /// <para>
  /// This method may be called from any thread, but the caller must ensure
  /// that the pointer is not used concurrently after it is freed.
  /// </para>
  /// </remarks>
  unsafe void Free(byte* ptr);
#pragma warning restore S6640 // Unsafe code blocks should not be used

  /// <summary>
  /// Rents a memory block from the pool and returns an <see cref="IOwner"/>
  /// handle that represents the lease.
  /// </summary>
  /// <returns>
  /// An <see cref="IOwner"/> instance that holds the rented block. The
  /// <see cref="IOwner.IsEmpty"/> property indicates whether the allocation
  /// succeeded.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This is a convenience wrapper over <see cref="Alloc"/> that creates an
  /// <see cref="IOwner"/> handle. It does <b>not</b> automatically release
  /// the block; the caller is responsible for disposing of the returned
  /// <see cref="IOwner"/> by calling its <see cref="IOwner.Dispose"/> method
  /// (typically via a <see langword="using"/> statement) to return the block
  /// to the pool.
  /// </para>
  /// <para>
  /// If no block is available, the returned <see cref="IOwner"/> will have a
  /// <see langword="null"/> pointer and zero size (i.e.,
  /// <see cref="IOwner.IsEmpty"/> returns <see langword="true"/>).
  /// </para>
  /// <para>
  /// The caller should always dispose of the <see cref="IOwner"/> as soon as
  /// the block is no longer needed to avoid memory leaks.
  /// </para>
  /// <example>
  /// Renting a block and safely returning it to the pool:
  /// <code>
  /// using (IOwner owner = pool.Rent()) {
  ///   if (!owner.IsEmpty) {
  ///     // Do your work with the block
  ///   }
  /// } // Automatically calls owner.Dispose() here
  /// </code>
  /// </example>
  /// </remarks>
  IOwner Rent();
}

/// <summary>
/// Represents a rented block of memory from a Pool.
/// </summary>
/// <remarks>
/// <para>
/// This is a <see langword="readonly"/> <see langword="struct"/> that holds a
/// pointer to a memory block and its size. It is designed to be lightweight
/// and allocation-free.
/// </para>
/// <para>
/// This type implements <see cref="IDisposable"/> and is intended to be used
/// within a <c>using</c> statement or <c>using</c> declaration to ensure
/// timely return of the rented block to the pool.
/// </para>
/// <para>
/// This struct is a value type; copying it creates independent copies of the
/// pointer and size. However, all copies refer to the <em>same</em> memory
/// block.
/// Calling <see cref="Dispose"/> on any copy returns the block to the pool,
/// rendering the pointer in <b>all</b> copies invalid. Do not access
/// <see cref="Span"/>, <see cref="Ptr"/>, or the indexer after disposal.
/// </para>
/// <para>
/// <see cref="Dispose"/> must be called exactly once for each rented block.
/// Duplicate calls may cause undefined behavior (depending on the pool
/// implementation) and failing to call it leaks memory.
/// </para>
/// <para>
/// For safe bounds-checked access, use the <see cref="Span"/> property
/// instead of the indexer <see cref="this[nuint]"/>.
/// </para>
/// <para>
/// Do not let <paramref name="size"/> larger than <see cref="int.MaxValue"/>.
/// </para>
/// </remarks>
#pragma warning disable S6640 // Unsafe code blocks should not be used
public readonly unsafe struct IOwner(IPool parent, byte* ptr, uint size)
  : IDisposable {
#pragma warning restore S6640 // Unsafe code blocks should not be used
  private readonly IPool? _parent = parent;

  /// <summary>
  /// Gets the pointer to the memory block.
  /// </summary>
  /// <value>
  /// A pointer to the start of the memory block, or <see langword="null"/>.
  /// </value>
  /// <remarks>
  /// This pointer is valid only for the duration of the lease. After
  /// <see cref="Dispose"/> is called, the pointer is no longer valid and
  /// should not be dereferenced.
  /// </remarks>
  public readonly byte* Ptr = ptr;

  /// <summary>
  /// Gets the size of the memory block in bytes.
  /// </summary>
  /// <value>
  /// The size in bytes. May be zero if the block is empty.
  /// </value>
  public readonly uint Size = size;

  /// <summary>
  /// Gets a value indicating whether this instance is empty.
  /// </summary>
  /// <value>
  /// <see langword="true"/> if the pointer is <see langword="null"/> or the
  /// size is zero; otherwise, <see langword="false"/>.
  /// </value>
  /// <remarks>
  /// This property does <em>not</em> indicate whether the block has been
  /// disposed.
  /// After disposal, <see cref="IsEmpty"/> may remain <see langword="false"/>
  /// even though the pointer is no longer valid.
  /// </remarks>
  public bool IsEmpty => this.Ptr is null || this.Size is 0;

  /// <summary>
  /// Gets a <see cref="Span{T}"/> over the rented memory block.
  /// </summary>
  /// <value>A Span&lt;byte&gt; representing the entire block.</value>
  /// <remarks>
  /// This property provides bounds-checked access and is the recommended way
  /// to read or write the rented memory.
  /// <para>
  /// Accessing this property after <see cref="Dispose"/> has been called is
  /// undefined behavior and may corrupt application state. Always ensure the
  /// instance is not disposed before using the span.
  /// </para>
  /// </remarks>
  public Span<byte> Span => new(this.Ptr,
    (int)this.Size); // we dont need add check int.MaxValuel is very big

  /// <summary>
  /// Unchecked indexer – use <see cref="Span"/> for bounds‑checked safety.
  /// </summary>
  /// <param name="index">The byte offset from the start of the block.</param>
  /// <value>The byte at the specified index.</value>
  /// <remarks>
  /// This accessor performs no bounds checking. Accessing an index outside
  /// the allocated range, or using the indexer after disposal, results in
  /// undefined behavior. Prefer using <see cref="Span"/> for safe access.
  /// </remarks>
  public byte this[nuint index] {
    get => this.Ptr[index];
    set => this.Ptr[index] = value;
  }

  /// <summary>
  /// Returns the rented memory block to the pool.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This method should be called exactly once per allocated block.
  /// Calling <see cref="Dispose"/> on any copy returns the block to the pool,
  /// invalidating the pointer in all copies of this instance.
  /// </para>
  /// <para>
  /// After disposal, the pointer and span are no longer valid for use.
  /// </para>
  /// <para>
  /// This method is not thread‑safe. Do not call <see cref="Dispose"/>
  /// concurrently on the same instance or its copies.
  /// </para>
  /// </remarks>
  public void Dispose() => this._parent?.Free(this.Ptr);
}

/// <summary>
/// A memory pool that allocates fixed‑size blocks from a single contiguous
/// native memory buffer, using a stack‑based free list stored at the end of
/// the buffer.
/// </summary>
/// <remarks>
/// <para>
/// The pool owns the underlying memory and releases it upon disposal.
/// Each block has a fixed size (<see cref="BlockSize"/>), and the total
/// number of blocks is <see cref="BlockCount"/>.
/// </para>
/// <para>
/// A free‑list stack (stored as an array of <see cref="ushort"/> indices) is
/// placed immediately after the data blocks. The stack uses a simple LIFO
/// policy, which provides fast allocation and deallocation.
/// </para>
/// <para>
/// All public methods are thread‑safe and use an internal lock.
/// </para>
/// </remarks>
#pragma warning disable S6640 // Unsafe code blocks should not be used
public sealed unsafe class PagePool : IDisposable, IPool {
#pragma warning restore S6640 // Unsafe code blocks should not be used

  /// <inheritdoc />
  public uint TotalByte { get; }
  /// <inheritdoc />
  public uint BlockSize { get; }
  /// <inheritdoc />
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
  /// <see langword="null"/>, indicating the system failed to allocate the
  /// requested memory.
  /// </exception>
  /// <remarks>
  /// <para>
  /// Powers of two are recommended for <paramref name="block_size"/> to
  /// enable optimization of division operations via bit shifting in hot paths
  /// (e.g., <see cref="Free"/>).
  /// </para>
  /// <para>
  /// The number of blocks is calculated as
  /// <c>floor(total_byte / (block_byte + 2))</c>, where <c>+2</c> accounts
  /// for one <see cref="ushort"/> index per block in metadata.
  /// Any remaining bytes are absorbed into the metadata section, ensuring
  /// zero wasted space.
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
    ArgumentOutOfRangeException.ThrowIfZero(size);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 2048);

    ArgumentOutOfRangeException.ThrowIfZero(block_size);

    uint total_byte = size * MiB;
    uint block_byte = block_size * 4 * KiB;

    uint theoretical = total_byte / (block_byte + 2);
    ArgumentOutOfRangeException.ThrowIfLessThan(theoretical, 2u);

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
  /// Finalizes an instance of the <see cref="PagePool"/> class.
  /// </summary>
  /// <remarks>
  /// This destructor releases the underlying native memory if the pool has
  /// not been explicitly disposed. It is provided as a safety net and should
  /// not be relied upon for timely resource cleanup;
  /// call <see cref="Dispose"/> explicitly instead.
  /// </remarks>
  ~PagePool() {
    // Cover: This branch is deliberately unreachable in tests.
    //  SuppressFinalize is always called after setting _disposed = true.
    // Removing this line would risk silent process crash (double-free) on
    // finalizer thread.
    if (this._disposed) return;
    NativeMemory.AlignedFree(this._pool);
  }

  /// <summary>
  /// Allocates a free data block from the pool.
  /// </summary>
  /// <returns>
  /// A pointer to the allocated block, or <see langword="null"/> if the pool
  /// is exhausted or disposed.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method is thread-safe and uses an internal lock.
  /// The returned pointer must be released with <see cref="Free"/> when no
  /// longer needed.
  /// </para>
  /// <para>
  /// The block is not zero-initialized; it contains whatever data was left
  /// from previous usage. Callers should overwrite the entire block before
  /// relying on its content.
  /// </para>
  /// </remarks>
  public byte* Alloc() {
    if (this._disposed) return null;
    lock (this._lock)
      if (!this._disposed && this.TryPop(out ushort index))
        return this._pool + (index * this.BlockSize);

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
  /// This method is thread-safe. It acquires the internal lock only when the
  /// pointer is valid, so invalid calls are cheap.
  /// </para>
  /// <para>
  /// After calling <see cref="Dispose"/>, this method returns silently
  /// without freeing any memory.
  /// </para>
  /// </remarks>
  public void Free(byte* ptr) {
    byte* pool = this._pool;
    if (this._disposed || ptr < pool || ptr >= pool + this.TotalByte) return;

    uint offset = (uint)(ptr - pool);
    (uint index, uint r) = Math.DivRem(offset, this.BlockSize);

    if (r is not 0 || index >= this.BlockCount) return;

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
  /// This method calls <see cref="Alloc"/> internally and wraps the result in
  /// an <see cref="IOwner"/>.
  /// </para>
  /// <para>
  /// The caller must call <see cref="IOwner.Dispose"/> on the returned
  /// instance to return the block to the pool.
  /// Failure to do so will leak the block.
  /// </para>
  /// <para>
  /// The returned <see cref="IOwner"/> is a readonly struct and can be
  /// copied, but only one copy should be disposed.
  /// </para>
  /// </remarks>
  public IOwner Rent() {
    byte* ptr = this.Alloc();
    return ptr is null ? default : new IOwner(this, ptr, this.BlockSize);
  }

  /// <summary>
  /// Releases all resources used by the <see cref="PagePool"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This method frees the underlying native memory and marks the pool as
  /// disposed. After disposal, all subsequent calls to <see cref="Alloc"/>,
  /// <see cref="Free"/>, and <see cref="Rent"/> will either return
  /// <see langword="null"/> or do nothing.
  /// </para>
  /// <para>
  /// This method is thread‑safe and may be called multiple times; subsequent
  /// calls are no‑ops.
  /// </para>
  /// <para>
  /// Calling <see cref="Dispose"/> suppresses finalization, so the destructor
  /// will not be invoked.
  /// </para>
  /// </remarks>
  public void Dispose() {
    if (this._disposed) return;

    lock (this._lock) {
      this._disposed = true;
      NativeMemory.AlignedFree(this._pool);
    }
    GC.SuppressFinalize(this);
  }

  // stack
  private void TryPush(ushort item) {
    if (this._top == this.BlockCount) return;
    this._items[this._top] = item;
    this._top++;
  }

  private bool TryPop(out ushort item) {
    if (this._top is 0) {
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
/// The total buffer size is specified in units of 4 KiB, and each block size
/// is given in units of 64 bytes. The block count is calculated such that the
/// bitmap fits exactly in the remaining space after all blocks.
/// The formula used is:
/// <c>block_count = floor((8 * total_byte - 7) / (8 * block_byte + 1))</c>.
/// This ensures the bitmap occupies the smallest possible number of bytes
/// while still addressing every block.
/// </para>
/// <para>
/// All public methods are thread‑safe and use an internal lock. The bitmap is
/// zero‑initialized at construction, indicating all blocks are free.
/// </para>
/// <para>
/// Because the buffer is owned externally, <see cref="Dispose"/> only marks
/// the pool as disposed and does <em>not</em> release the buffer memory.
/// </para>
/// </remarks>
#pragma warning disable S6640 // Unsafe code blocks should not be used
public sealed unsafe class CachePool : IDisposable, IPool {
#pragma warning restore S6640 // Unsafe code blocks should not be used

  /// <inheritdoc />
  public uint TotalByte { get; }
  /// <inheritdoc />
  public uint BlockSize { get; }
  /// <inheritdoc />
  public uint BlockCount { get; }

  private bool _disposed = false;

  private readonly ulong* _map;
  private readonly byte* _ptr;

  private readonly Lock _lock = new();

  /// <summary>
  /// Creates a new <see cref="CachePool"/> instance from a managed span of
  /// bytes.
  /// </summary>
  /// <param name="sp">
  /// A span of bytes representing the pre‑allocated buffer. The buffer must
  /// be large enough to hold both the data blocks and the bitmap.
  /// Its length is converted to units of 4 KiB (i.e., <c>sp.Length / 4096</c>)
  /// to derive the size parameter of the constructor.
  /// </param>
  /// <param name="block_size">
  /// Size of each memory block in units of 64 bytes
  /// (i.e., <c>block_size * 64</c> bytes).
  /// Must be greater than 0. The default value is 1.
  /// </param>
  /// <returns>
  /// A new <see cref="CachePool"/> instance that operates on the provided
  /// buffer.
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when:
  /// <list type="bullet">
  /// <item><paramref name="block_size"/> is 0.</item>
  /// <item>
  /// The buffer size (derived from <paramref name="sp"/>.Length) is too small
  /// to hold at least 2 blocks and the bitmap overhead, causing the
  /// constructor to throw.
  /// </item>
  /// </list>
  /// </exception>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="sp"/>.Length is zero.
  /// </exception>
  /// <remarks>
  /// <para>
  /// The pool does <em>not</em> take ownership of the buffer. Disposing the pool only
  /// marks it as disposed; the underlying memory is <em>not</em> released. The caller is
  /// fully responsible for managing the buffer's lifetime.
  /// </para>
  /// <para>
  /// <b>IMPORTANT:</b> The caller must ensure the span remains alive and
  /// unmodified for the lifetime of the<see cref="CachePool"/>.
  /// If the span wraps a managed array, it must be pinned
  /// (e.g., via<c>fixed</c>) before calling this method.
  /// </para>
  /// </remarks>
  public static CachePool Create(Span<byte> sp, ushort block_size = 1)
    => new((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(sp)),
      (ushort)(sp.Length / (4 * KiB)), block_size);

  /// <summary>
  /// Initializes a new instance of the <see cref="CachePool"/> class.
  /// </summary>
  /// <param name="ptr">
  /// Pointer to the start of a pre‑allocated buffer. The buffer must be large
  /// enough to hold both the data blocks and the bitmap.
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
    ArgumentNullException.ThrowIfNull(ptr);

    ArgumentOutOfRangeException.ThrowIfZero(size);
    ArgumentOutOfRangeException.ThrowIfZero(block_size);

    uint total_byte = size * 4u * KiB;
    uint block_byte = block_size * 64u;

    uint block_count = ((8 * total_byte) - 7) / ((8 * block_byte) + 1);

    ArgumentOutOfRangeException.ThrowIfLessThan(block_count, 2u);

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
  /// A pointer to the allocated block, or <see langword="null"/> if no block
  /// is available or the pool has been disposed.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method is thread‑safe.
  /// It scans the bitmap for the first free block, sets its bit to 1, and
  /// returns the corresponding pointer.
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
    var map = this._map;

    lock (this._lock) {
      if (this._disposed) return null;

      for (uint idx = 0; idx < count; idx++) {
        ulong word = map[idx];

        if (word is ulong.MaxValue) continue;

        ulong inverted = ~word;
        int bit = System.Numerics.BitOperations.TrailingZeroCount(inverted);

        uint block_idx = (idx * 64) + (uint)bit;
        if (block_idx < this.BlockCount) {
          map[idx] |= 1ul << bit;
          return this._ptr + (block_idx * this.BlockSize);
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
  /// If <paramref name="ptr"/> is <see langword="null"/>, out of the buffer
  /// range, not aligned to a block boundary, or its corresponding bit in the
  /// bitmap is already 0 (i.e., already free), this method does nothing.
  /// </para>
  /// <para>
  /// This method is thread‑safe. It acquires the internal lock only when the
  /// pointer is valid, so invalid calls are cheap.
  /// </para>
  /// <para>
  /// After the pool has been disposed, calls to this method return silently
  /// without modifying the bitmap.
  /// </para>
  /// </remarks>
  public void Free(byte* ptr) {
    if (this._disposed) return;

    byte* pool = this._ptr;
    if (ptr < pool || ptr >= pool + this.TotalByte) return;

    uint offset = (uint)(ptr - pool);
    uint index = offset / this.BlockSize;
    uint remainder = offset % this.BlockSize;

    if (remainder is not 0 || index >= this.BlockCount) return;

    uint wordIdx = index / 64;
    uint bitOffset = index & 63;
    ulong mask = 1UL << (int)bitOffset;

    var map = this._map;

    lock (this._lock) {
      if (this._disposed || (map[wordIdx] & mask) is 0) return;
      map[wordIdx] &= ~mask;
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
  /// This method calls <see cref="Alloc"/> internally and wraps the result in
  /// an <see cref="IOwner"/>.
  /// </para>
  /// <para>
  /// The caller must call <see cref="IOwner.Dispose"/> on the returned
  /// instance to return the block to the pool. Failure to do so will leak the
  /// block.
  /// </para>
  /// <para>
  /// The returned <see cref="IOwner"/> is a readonly struct and can be copied,
  /// but only one copy should be disposed.
  /// </para>
  /// </remarks>
  public IOwner Rent() {
    byte* ptr = this.Alloc();
    return ptr is null ? default : new IOwner(this, ptr, this.BlockSize);
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
