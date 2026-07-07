
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Core;

namespace Test;

public sealed class PagePoolTests : IDisposable {
  private readonly PagePool _pool;

  public PagePoolTests() => this._pool = new PagePool(size: 1, block_size: 1);

  public void Dispose() => this._pool?.Dispose();

  [Fact]
  public void Constructor_ValidParameters_CreatesPool() {
    Assert.Equal(1u * 1024u * 1024u, this._pool.TotalByte);
    Assert.Equal(1u * 4u * 1024u, this._pool.BlockSize);
    Assert.True(this._pool.BlockCount >= 2);
  }

  [Theory]
  [InlineData(0, 1)]
  [InlineData(2049, 1)]
  [InlineData(1, 0)]
  public void Constructor_InvalidArguments_ThrowsArgumentOutOfRange(
    ushort size, uint blockSize)
    => Assert.Throws<ArgumentOutOfRangeException>(
      () => new PagePool(size, blockSize));

  [Fact]
  public unsafe void Alloc_WhenPoolHasBlocks_ReturnsNonNullPointer() {
    byte* ptr = this._pool.Alloc();
    Assert.True(ptr != null);
    this._pool.Free(ptr);
  }

  [Fact]
  public unsafe void Alloc_WhenPoolExhausted_ReturnsNull() {
    var pointers = new List<IntPtr>();
    for (int i = 0; i < this._pool.BlockCount; i++) {
      byte* p = this._pool.Alloc();
      Assert.True(p != null);
      pointers.Add((IntPtr)p);
    }

    byte* last = this._pool.Alloc();
    Assert.True(last == null);

    this._pool.Free((byte*)pointers[0]);
    byte* reused = this._pool.Alloc();
    Assert.True(reused != null);
    this._pool.Free(reused);
  }

  [Fact]
  public unsafe void Free_InvalidPointer_DoesNothing() {
    this._pool.Free(null);
    byte* outside = (byte*)IntPtr.Zero + 1000000;
    this._pool.Free(outside);

    byte* ptr = this._pool.Alloc();
    byte* misaligned = ptr + 1;
    this._pool.Free(misaligned);
    this._pool.Free(ptr);
  }

  [Fact]
  public unsafe void Free_AfterDispose_DoesNothing() {
    byte* ptr = this._pool.Alloc();
    Assert.True(ptr != null);
    this._pool.Dispose();
    this._pool.Free(ptr);
    Assert.True(this._pool.Alloc() == null);
  }

  [Fact]
  public unsafe void Rent_ReturnsIOwner_WithCorrectSizeAndPointer() {
    IOwner owner = this._pool.Rent();
    Assert.False(owner.IsEmpty);
    Assert.Equal(this._pool.BlockSize, owner.Size);
    Assert.True(owner.Ptr != null);

    Span<byte> span = owner.Span;
    span[0] = 0xAA;
    Assert.Equal(0xAA, *owner.Ptr);

    owner.Dispose();
    IOwner owner2 = this._pool.Rent();
    Assert.False(owner2.IsEmpty);
    owner2.Dispose();
  }

  [Fact]
  public void Rent_WhenPoolEmpty_ReturnsDefault() {
    var owners = new List<IOwner>();
    for (int i = 0; i < this._pool.BlockCount; i++) {
      IOwner o = this._pool.Rent();
      Assert.False(o.IsEmpty);
      owners.Add(o);
    }

    IOwner empty = this._pool.Rent();
    Assert.True(empty.IsEmpty);

    owners[0].Dispose();
    IOwner reused = this._pool.Rent();
    Assert.False(reused.IsEmpty);
    reused.Dispose();
  }

  [Fact]
  public unsafe void IOwner_Indexer_UncheckedAccess() {
    IOwner owner = this._pool.Rent();
    try {
      owner[0] = 0xAB;
      Assert.Equal(0xAB, owner[0]);
    }
    finally {
      owner.Dispose();
    }
  }

  [Fact]
  public void Dispose_MultipleCalls_IsSafe() {
    IOwner owner = this._pool.Rent();
    owner.Dispose();
    owner.Dispose();
  }

  [Fact]
  public async Task PagePool_ThreadSafety_ConcurrentAllocFreeAsync() {
    const int iterations = 100;
    var tasks = new List<Task>();
    for (int t = 0; t < 8; t++) {
      tasks.Add(Task.Run(() => {
        for (int i = 0; i < iterations; i++) {
          unsafe {
            byte* ptr = this._pool.Alloc();
            if (ptr != null) {
              *ptr = (byte)i;
              this._pool.Free(ptr);
            }
            else {
              Task.Delay(1).Wait();
            }
          }
        }
      }));
    }

    await Task.WhenAll(tasks);
  }
}

public sealed class CachePoolTests : IDisposable {
  private const ushort BufferSize = 8;
  private const ushort BlockSize = 1;
  private unsafe byte* _buffer;
  private readonly CachePool _pool;

  public unsafe CachePoolTests() {
    this._buffer = (byte*)Marshal.AllocHGlobal((IntPtr)(BufferSize * 4 * 1024));
    this._pool = new CachePool(this._buffer, BufferSize, BlockSize);
  }

  public unsafe void Dispose() {
    this._pool?.Dispose();
    if (this._buffer != null) {
      Marshal.FreeHGlobal((IntPtr)this._buffer);
      this._buffer = null;
    }
  }

  [Fact]
  public void Constructor_ValidParameters_CreatesPool() {
    uint totalByte = BufferSize * 4u * 1024u;
    uint blockByte = BlockSize * 64u;
    Assert.Equal(totalByte, this._pool.TotalByte);
    Assert.Equal(blockByte, this._pool.BlockSize);
    Assert.True(this._pool.BlockCount >= 2);
  }

  [Theory]
  [InlineData(0, 1)]
  [InlineData(1, 0)]
  public void Constructor_InvalidArguments_Throws(ushort size, ushort blockSize) {
    unsafe {
      byte* dummy = (byte*)Marshal.AllocHGlobal(4096);
      try {
        if (size == 0 || blockSize == 0)
          Assert.Throws<ArgumentOutOfRangeException>(
            () => new CachePool(dummy, size, blockSize));
      }
      finally {
        Marshal.FreeHGlobal((IntPtr)dummy);
      }
    }
  }

  [Fact]
  public void Constructor_NullPointer_ThrowsArgumentNull() {
    unsafe {
      Assert.Throws<ArgumentNullException>(
        () => new CachePool(null, 8, 1));
    }
  }

  [Fact]
  public unsafe void Alloc_WhenBlocksAvailable_ReturnsPointer() {
    byte* ptr = this._pool.Alloc();
    Assert.True(ptr != null);
    this._pool.Free(ptr);
  }

  [Fact]
  public unsafe void Alloc_Exhaustion_ReturnsNull() {
    var pointers = new List<IntPtr>();
    for (int i = 0; i < this._pool.BlockCount; i++) {
      byte* p = this._pool.Alloc();
      Assert.True(p != null);
      pointers.Add((IntPtr)p);
    }

    byte* last = this._pool.Alloc();
    Assert.True(last == null);

    this._pool.Free((byte*)pointers[0]);
    byte* reused = this._pool.Alloc();
    Assert.True(reused != null);
    this._pool.Free(reused);
  }

  [Fact]
  public unsafe void Free_InvalidPointer_DoesNothing() {
    this._pool.Free(null);
    byte* outside = this._buffer + this._pool.TotalByte + 10;
    this._pool.Free(outside);

    byte* ptr = this._pool.Alloc();
    byte* misaligned = ptr + 1;
    this._pool.Free(misaligned);
    this._pool.Free(ptr);
    this._pool.Free(ptr);
  }

  [Fact]
  public unsafe void Rent_ReturnsIOwner_WithCorrectValues() {
    IOwner owner = this._pool.Rent();
    Assert.False(owner.IsEmpty);
    Assert.Equal(this._pool.BlockSize, owner.Size);
    Assert.True(owner.Ptr != null);

    Span<byte> span = owner.Span;
    span[0] = 0xCD;
    Assert.Equal(0xCD, *owner.Ptr);

    owner.Dispose();
    IOwner owner2 = this._pool.Rent();
    Assert.False(owner2.IsEmpty);
    owner2.Dispose();
  }

  [Fact]
  public void Dispose_MarksPoolDisposed_AllocReturnsNull() {
    this._pool.Dispose();
    unsafe {
      Assert.True(this._pool.Alloc() == null);
      this._pool.Free(this._buffer);
    }
  }

  [Fact]
  public async Task CachePool_ThreadSafety_ConcurrentAccessAsync() {
    const int iterations = 50;
    var tasks = new List<Task>();
    for (int t = 0; t < 8; t++) {
      tasks.Add(Task.Run(() => {
        for (int i = 0; i < iterations; i++) {
          unsafe {
            byte* ptr = this._pool.Alloc();
            if (ptr != null) {
              *ptr = (byte)i;
              this._pool.Free(ptr);
            }
            else {
              Task.Delay(1).Wait();
            }
          }
        }
      }));
    }

    await Task.WhenAll(tasks);
  }
}