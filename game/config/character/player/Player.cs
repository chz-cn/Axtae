
using System.Runtime.CompilerServices;

namespace Game.Config.Character.Player;

public struct Player() {
  public enum Form : byte { Normal, Armed }
  public enum ShotPattern : byte { Normal, Spiral }

#pragma warning disable S3604 // Member initializer values should not be
  // redundant
  public float BaseMoveSpeed { readonly get; set; } = 48f;
  public float BaseShootDelay { readonly get; set; } = .4f;
  public float BaseSpiralShootDelay { readonly get; set; } = .01f;

  public ushort BaseSpiralBullets { readonly get; set; } = 64;
  public byte BaseSpiralBulletsPerCircle { readonly get; set; } = 16;

  public float BaseSpeedMultiplier { readonly get; set; } = 1;
  public float BaseFireRateMultiplier { readonly get; set; } = 1;

  public Form FormMode { readonly get; set; } = Form.Normal;
  public ShotPattern ShotPatternMode { readonly get; set; } = ShotPattern.Normal;
#pragma warning restore S3604 // Member initializer values should not be
  // redundant

  public record struct Buff<T>(float EndLifeTime, T Value);

  private double _time = 0;

  public void Update(double delta) => this._time += delta;

#pragma warning disable S3459 // Unassigned members should be removed
  private InlineArray3<Buff<float>> _bMoveSpeed;
  private InlineArray3<Buff<float>> _bShootDelay;
  private InlineArray3<Buff<float>> _bSpiralShootDelay;
  private InlineArray3<Buff<float>> _bSpeedMultiplier;
  private InlineArray3<Buff<float>> _bFireRateMultiplier;
  private InlineArray3<Buff<ushort>> _bSpiralBullets;
  private InlineArray3<Buff<byte>> _bSpiralBulletsPerCircle;
#pragma warning restore S3459 // Unassigned members should be removed

  public readonly float MoveSpeed {
    get {
      float add = 0;
      foreach (var (time, val) in this._bMoveSpeed)
        if (time > this._time) add += val;

      return this.BaseMoveSpeed + add;
    }
  }

  public readonly float ShootDelay {
    get {
      float add = 0;
      foreach (var buff in this._bShootDelay)
        if (buff.EndLifeTime > this._time) add += buff.Value;
      return this.BaseShootDelay + add;
    }
  }

  public readonly float SpiralShootDelay {
    get {
      float add = 0;
      foreach (var buff in this._bSpiralShootDelay)
        if (buff.EndLifeTime > this._time) add += buff.Value;
      return this.BaseSpiralShootDelay + add;
    }
  }

  public readonly float SpeedMultiplier {
    get {
      float mul = 1;
      foreach (var buff in this._bSpeedMultiplier)
        if (buff.EndLifeTime > this._time) mul *= buff.Value;
      return this.BaseSpeedMultiplier * mul;
    }
  }

  public readonly float FireRateMultiplier {
    get {
      float mul = 1;
      foreach (var buff in this._bFireRateMultiplier)
        if (buff.EndLifeTime > this._time) mul *= buff.Value;
      return this.BaseFireRateMultiplier * mul;
    }
  }

  public readonly ushort SpiralBullets {
    get {
      uint add = 0;
      foreach (var buff in this._bSpiralBullets)
        if (buff.EndLifeTime > this._time) add += buff.Value;

      uint res = this.BaseSpiralBulletsPerCircle + add;
      return res > ushort.MaxValue ? ushort.MaxValue : (byte)res;
    }
  }

  public readonly byte SpiralBulletsPerCircle {
    get {
      uint add = 0;
      foreach (var buff in this._bSpiralBulletsPerCircle)
        if (buff.EndLifeTime > this._time) add += buff.Value;

      uint res = this.BaseSpiralBulletsPerCircle + add;
      return res > byte.MaxValue ? byte.MaxValue : (byte)res;
    }
  }

  public void AddSpeed(float value, float duration) {
    if (!float.IsNormal(value)) return;
    this._bMoveSpeed[FindMinPosition(this._bMoveSpeed)]
      = new((float)this._time + duration, value);
  }

  public void AddShootDelay(float value, float duration) {
    if (!float.IsNormal(value)) return;
    this._bShootDelay[FindMinPosition(this._bShootDelay)]
      = new((float)this._time + duration, value);
  }

  public void AddSpiralShootDelay(float value, float duration) {
    if (!float.IsNormal(value)) return;
    this._bSpiralShootDelay[FindMinPosition(this._bSpiralShootDelay)]
      = new((float)this._time + duration, value);
  }

  public void AddSpeedMultiplier(float value, float duration) {
    if (!float.IsNormal(value)) return;
    this._bSpeedMultiplier[FindMinPosition(this._bSpeedMultiplier)]
      = new((float)this._time + duration, value);
  }

  public void AddFireRateMultiplier(float value, float duration) {
    if (!float.IsNormal(value)) return;
    this._bFireRateMultiplier[FindMinPosition(this._bFireRateMultiplier)]
     = new((float)this._time + duration, value);
  }

  public void AddSpiralBullets(ushort value, float duration) {
    if (value is 0) return;
    this._bSpiralBullets[FindMinPosition(this._bSpiralBullets)]
     = new((float)this._time + duration, value);
  }

  public void AddSpiralBulletsPerCircle(byte value, float duration) {
    if (value is 0) return;
    this._bSpiralBulletsPerCircle[FindMinPosition(
     this._bSpiralBulletsPerCircle)]
     = new((float)this._time + duration, value);
  }

  private static int FindMinPosition<T>(InlineArray3<Buff<T>> buff) {
    var ((a, _), (b, _), (c, _)) = (buff[0], buff[1], buff[2]);
    if (a <= b && a <= c) return 0;
    if (b <= a && b <= c) return 1;
    return 2;
  }
}
