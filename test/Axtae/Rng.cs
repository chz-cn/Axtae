
using System.Threading.Tasks;
using Axtae;
using Axtae.Random;

namespace Test;

public sealed class RngTests {
  [Fact]
  public void Shared_ReturnsInRange() {
    var rng = Rng.Shared;
    for (int i = 0; i < 100; i++)
      Assert.InRange(rng.NextUInt64(), ulong.MinValue, ulong.MaxValue);
  }

  [Fact]
  public void Extensions_Work() {
    var rng = Rng.Shared;
    Assert.Equal(0UL, rng.NextUInt64(0));

    for (ulong max = 1; max <= 10; max++)
      Assert.InRange(rng.NextUInt64(max), 0UL, max - 1);

    Assert.InRange(rng.NextDouble(), 0.0, 1.0);
    Assert.NotEqual(1.0, rng.NextDouble());
    Assert.InRange(rng.NextDoubleInclusive(), 0.0, 1.0);
  }

  [Fact]
  public async Task ConcurrentAccess_NoException() {
    var ex = await Record.ExceptionAsync(static async () => {
      var tasks = new Task[10];
      for (int i = 0; i < tasks.Length; i++)
        tasks[i] = Task.Run(static () => {
          for (int j = 0; j < 100; j++)
            _ = Rng.Shared.NextUInt64();
        });

      await Task.WhenAll(tasks);
    });

    Assert.Null(ex);
  }

  // ===== exts =====
  [Fact]
  public void NextUInt64_Max_ZeroReturnsZero() {
    var rng = Rng.Shared;
    Assert.Equal(0UL, rng.NextUInt64(0));
  }

  [Fact]
  public void NextUInt64_Max_ReturnsInRange() {
    var rng = Rng.Shared;
    for (ulong max = 1; max <= 10; max++)
      for (int i = 0; i < 20; i++) {
        ulong v = rng.NextUInt64(max);
        Assert.InRange(v, 0u, max - 1);
      }
  }

  [Fact]
  public void NextUInt64_Max_Rejection_Sampling() {
    var rng = Rng.Shared;
    const ulong Max = (ulong.MaxValue / 2) + 2;

    for (int i = 0; i < 50; i++) {
      ulong v = rng.NextUInt64(Max);
      Assert.InRange(v, 0u, Max - 1);
    }
  }

  [Fact]
  public void NextUInt64_MinMax_InvalidRangeReturnsZero() {
    var rng = Rng.Shared;
    Assert.Equal(0u, rng.NextUInt64(5, 3));
    Assert.Equal(0u, rng.NextUInt64(5, 5));
  }

  [Fact]
  public void NextUInt64_MinMax_ReturnsInRange() {
    var rng = Rng.Shared;

    for (ulong min = 0; min < 5; min++)
      for (ulong max = min + 1; max <= min + 10; max++)
        for (int i = 0; i < 20; i++)
          Assert.InRange(rng.NextUInt64(min, max), min, max - 1);
  }

  [Fact]
  public void NextDouble_ReturnsInRange() {
    var rng = Rng.Shared;
    for (int i = 0; i < 50; i++) {
      var d = rng.NextDouble();
      Assert.InRange(d, 0, 1);
      Assert.NotEqual(1, d);
    }
  }

  [Fact]
  public void NextDoubleInclusive_ReturnsInRange() {
    var rng = Rng.Shared;

    for (int i = 0; i < 50; i++)
      Assert.InRange(rng.NextDoubleInclusive(), 0, 1);
  }
}
