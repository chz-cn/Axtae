
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Axtae;

namespace Test;

public sealed class IOwnerTests {
  [Fact]
  public void Dispose_WhenNull() {
    var ex = Record.Exception(static () => {
      IOwner owner = default;
      owner.Dispose();
    });
    Assert.Null(ex);
  }
}

#pragma warning disable S6640 // Unsafe code blocks should not be used

public sealed class PagePoolTests : IDisposable {
  private readonly PagePool _pool = new(1, 1);

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
  public void Constructor_InvalidArguments_ThrowsArgumentOutOfRange
    (ushort size, uint blockSize)
    => Assert.Throws<ArgumentOutOfRangeException>(
      () => new PagePool(size, blockSize));

  [Fact]
  public void Alloc_WhenPoolHasBlocks_ReturnsNonNullPointer() {
    using var ptr = this._pool.Rent();
    Assert.False(ptr.IsEmpty);
  }

  [Fact]
  public void Alloc_WhenPoolExhausted_ReturnsNull() {
    var owner = this._pool.Rent();
    for (int i = 1; i < this._pool.BlockCount; i++)
      Assert.False(this._pool.Rent().IsEmpty);

    var last = this._pool.Rent();
    Assert.True(last.IsEmpty);

    owner.Dispose();
    using var reused = this._pool.Rent();
    Assert.False(reused.IsEmpty);
  }

  [Fact]
  public unsafe void Free_InvalidPointer_DoesNothing() {
    var exception = Record.Exception(() => {
      this._pool.Free(null);
      byte* outside = (byte*)nuint.Zero + 1000000;
      this._pool.Free(outside);

      byte* ptr = this._pool.Alloc();
      byte* misaligned = ptr + 1;
      this._pool.Free(misaligned);
      this._pool.Free(ptr);
    });
    Assert.Null(exception);
  }

  [Fact]
  public void Free_AfterDispose_DoesNothing() {
    var owner = this._pool.Rent();
    Assert.False(owner.IsEmpty);

    this._pool.Dispose();
    owner.Dispose();
    Assert.True(this._pool.Rent().IsEmpty);
  }

  [Fact]
  public void Rent_ReturnsIOwner_WithCorrectSizeAndPointer() {
    IOwner owner = this._pool.Rent();
    Assert.False(owner.IsEmpty);
    Assert.Equal(this._pool.BlockSize, owner.Size);

    Span<byte> span = owner.Span;
    span[0] = 0xAA;
    Assert.Equal(0xAA, owner[0]);

    owner.Dispose();
    IOwner owner2 = this._pool.Rent();
    Assert.False(owner2.IsEmpty);
    owner2.Dispose();
  }

  [Fact]
  public void Rent_WhenPoolEmpty_ReturnsDefault() {
    var owners = new List<IOwner>((int)this._pool.BlockCount);
    for (int i = 0; i < this._pool.BlockCount; i++) {
      IOwner o = this._pool.Rent();
      Assert.False(o.IsEmpty);
      owners.Add(o);
    }

    using IOwner empty = this._pool.Rent();
    Assert.True(empty.IsEmpty);

    owners[0].Dispose();
    using IOwner reused = this._pool.Rent();
    Assert.False(reused.IsEmpty);
  }

  [Fact]
  public void IOwner_Indexer_UncheckedAccess() {
    using IOwner owner = this._pool.Rent();
    owner[0] = 0xAB;
    Assert.Equal(0xAB, owner[0]);
  }

  [Fact]
  public async Task PagePool_ThreadSafety_ConcurrentAllocFreeAsync() {
    const int Iterations = 100;

    var tasks = new InlineArray8<Task>();
    foreach (ref var t in tasks)
      t = Task.Run(async () => {
        for (int i = 0; i < Iterations; i++) {
          using IOwner owner = this._pool.Rent();
          if (!owner.IsEmpty)
            owner[0] = 1;
          else
            await Task.Yield();
        }
      });

    var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
    Assert.Null(exception);
  }

  [Fact]
  public void FinalizeAfterDispose_DoesNothing() {
    this._pool.Dispose();
    GC.ReRegisterForFinalize(this._pool);

    Assert.True(true);
  }

  [Fact]
  public async Task Alloc_Free_DoubleCheck_ConcurrentRace() {
    const int Iterations = 1000;

    var tasks = new InlineArray11<Task>();

    for (int attempt = 0; attempt < Iterations; attempt++) {
      using var pool = new PagePool(1, 1);

      foreach (ref var task in tasks[..10])
        task = Task.Run(() => pool.Rent().Dispose());

      tasks[10] = Task.Run(pool.Dispose);

      await Task.WhenAll(tasks);
    }

    Assert.True(true);
  }
}

public sealed class CachePoolTests {
  private const ushort BufferSize = 8;
  private const ushort BlockSize = 1;
  private readonly byte[] _buffer;
  private readonly CachePool _pool;

  public CachePoolTests() {
    this._buffer = GC.AllocateUninitializedArray<byte>
      (BufferSize * 4 * 1024, true);
    this._pool = CachePool.Create(this._buffer, BlockSize);
  }

  [Fact]
  public void Constructor_ValidParameters_CreatesPool() {
    const uint TotalByte = BufferSize * 4 * 1024;
    const uint BlockByte = BlockSize * 64;
    Assert.Equal(TotalByte, this._pool.TotalByte);
    Assert.Equal(BlockByte, this._pool.BlockSize);
    Assert.True(this._pool.BlockCount >= 2);
  }

  [Theory]
  [InlineData(0, 1)]
  [InlineData(1, 0)]
  public void Constructor_InvalidArguments_Throws(ushort size, ushort blockSize) {
    var dummy = GC.AllocateUninitializedArray<byte>(size, true);
    if (size is 0 || blockSize is 0)
      _ = Assert.Throws<ArgumentOutOfRangeException>(
        () => CachePool.Create(dummy, blockSize));
  }

  [Fact]
  public void Constructor_NullPointer_ThrowsArgumentNull() {
    _ = Assert.Throws<ArgumentNullException>(
      static () => CachePool.Create([]));
  }

  [Fact]
  public void Alloc_WhenBlocksAvailable_ReturnsPointer() {
    using var owner = this._pool.Rent();
    Assert.False(owner.IsEmpty);
  }

  [Fact]
  public void Alloc_Exhaustion_ReturnsNull() {
    var owner = this._pool.Rent();
    for (int i = 1; i < this._pool.BlockCount; i++)
      Assert.False(this._pool.Rent().IsEmpty);

    var last = this._pool.Rent();
    Assert.True(last.IsEmpty);

    owner.Dispose();
    var reused = this._pool.Rent();
    Assert.False(reused.IsEmpty);
  }

  [Fact]
  public unsafe void Free_InvalidPointer_DoesNothing() {
    var exception = Record.Exception(() => {
      this._pool.Free(null);
      byte* outside = (byte*)nuint.Zero + 1000000;
      this._pool.Free(outside);

      byte* ptr = this._pool.Alloc();
      byte* misaligned = ptr + 1;
      this._pool.Free(misaligned);
      this._pool.Free(ptr);
      this._pool.Free(ptr);
    });
    Assert.Null(exception);
  }

  [Fact]
  public async Task Alloc_Free_DoubleCheck_ConcurrentRace() {
    const int Iterations = 1000;
    const int Size = 4 * 1024;
    var buffer = GC.AllocateUninitializedArray<byte>(Size, true);

    var tasks = new InlineArray11<Task>();

    for (int attempt = 0; attempt < Iterations; attempt++) {
      using var pool = CachePool.Create(buffer);

      foreach (ref var task in tasks[..10])
        task = Task.Run(() => pool.Rent().Dispose());

      tasks[10] = Task.Run(pool.Dispose);

      await Task.WhenAll(tasks);
    }

    Assert.True(true);
  }

  [Fact]
  public void Rent_ReturnsIOwner_WithCorrectValues() {
    IOwner owner = this._pool.Rent();
    Assert.False(owner.IsEmpty);
    Assert.Equal(this._pool.BlockSize, owner.Size);

    Span<byte> span = owner.Span;
    span[0] = 0xCD;
    Assert.Equal(0xCD, owner[0]);

    owner.Dispose();
    IOwner owner2 = this._pool.Rent();
    Assert.False(owner2.IsEmpty);
    owner2.Dispose();
  }

  [Fact]
  public void Dispose_MarksPoolDisposed_AllocReturnsNull() {
    this._pool.Dispose();
    Assert.True(this._pool.Rent().IsEmpty);
  }

  [Fact]
  public async Task CachePool_ThreadSafety_ConcurrentAccessAsync() {
    const int Iterations = 50;
    var tasks = new InlineArray8<Task>();
    foreach (ref var t in tasks)
      t = Task.Run(async () => {
        for (int i = 0; i < Iterations; i++) {
          using IOwner owner = this._pool.Rent();
          if (!owner.IsEmpty)
            owner[0] = 1;
          else
            await Task.Yield();
        }
      });

    var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
    Assert.Null(exception);
  }
}

#pragma warning restore S6640 // Unsafe code blocks should not be used
