
using System.Runtime.CompilerServices;

namespace Game.Config.Character.Player;

public struct Player() {
  public enum Form : byte { Normal, Armed }
  public enum ShotPattern : byte { Normal, Spiral }

  public float BaseMoveSpeed { readonly get; set; } = 48f;
  public float BaseShootDelay { readonly get; set; } = .4f;
  public float BaseSpiralShootDelay { readonly get; set; } = .01f;

  public ushort BaseSpiralBullets { readonly get; set; } = 64;
  public byte BaseSpiralBulletsPerCircle { readonly get; set; } = 16;

  public float BaseSpeedMultiplier { readonly get; set; } = 1;
  public float BaseFireRateMultiplier { readonly get; set; } = 1;

  public Form FormMode { readonly get; set; } = Form.Normal;
  public ShotPattern ShotPatternMode { readonly get; set; } = ShotPattern.Normal;

  public struct Buff<T> {
    public float EndLifeTime;
    public T Value;
  }

  private double _time = 0f;

  public void Update(double delta) => this._time += delta;

  private InlineArray3<Buff<float>> _bMoveSpeed;
  private InlineArray3<Buff<float>> _bShootDelay;
  private InlineArray3<Buff<float>> _bSpiralShootDelay;
  private InlineArray3<Buff<float>> _bSpeedMultiplier;
  private InlineArray3<Buff<float>> _bFireRateMultiplier;
  private InlineArray3<Buff<int>> _bSpiralBullets;
  private InlineArray3<Buff<int>> _bSpiralBulletsPerCircle;

  public readonly float MoveSpeed {
    get {
      float add = 0;
      foreach (var buff in this._bMoveSpeed)
        if (buff.EndLifeTime > this._time) add += buff.Value;

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
      float mul = 0;
      foreach (var buff in this._bSpeedMultiplier)
        if (buff.EndLifeTime > this._time) mul *= buff.Value;
      return this.BaseSpeedMultiplier * mul;
    }
  }

  public readonly float FireRateMultiplier {
    get {
      float mul = 0;
      foreach (var buff in this._bFireRateMultiplier)
        if (buff.EndLifeTime > this._time) mul *= buff.Value;
      return this.BaseFireRateMultiplier * mul;
    }
  }

  public readonly ushort SpiralBullets {
    get {
      int add = 0;
      foreach (var buff in this._bSpiralBullets)
        if (buff.EndLifeTime > this._time) add += buff.Value;
      return (ushort)(this.BaseSpiralBullets + add);
    }
  }

  public readonly byte SpiralBulletsPerCircle {
    get {
      int add = 0;
      foreach (var buff in this._bSpiralBulletsPerCircle)
        if (buff.EndLifeTime > this._time) add += buff.Value;
      return (byte)(this.BaseSpiralBulletsPerCircle + add);
    }
  }

  public void AddSpeed(float value, float duration) {
    var buff = new Buff<float> {
      EndLifeTime = (float)this._time + duration,
      Value = value
    };
    this._bMoveSpeed[FindMinPosition(this._bMoveSpeed)] = buff;
  }

  public void AddShootDelay(float value, float duration) {
    var buff = new Buff<float> {
      EndLifeTime = (float)this._time + duration,
      Value = value
    };
    this._bShootDelay[FindMinPosition(this._bShootDelay)] = buff;
  }

  public void AddSpiralShootDelay(float value, float duration) {
    var buff = new Buff<float> {
      EndLifeTime = (float)this._time + duration,
      Value = value
    };
    this._bSpiralShootDelay[FindMinPosition(this._bSpiralShootDelay)] = buff;
  }

  public void AddSpeedMultiplier(float value, float duration) {
    var buff = new Buff<float> {
      EndLifeTime = (float)this._time + duration,
      Value = value
    };
    this._bSpeedMultiplier[FindMinPosition(this._bSpeedMultiplier)] = buff;
  }

  public void AddFireRateMultiplier(float value, float duration) {
    var buff = new Buff<float> {
      EndLifeTime = (float)this._time + duration,
      Value = value
    };
    this._bFireRateMultiplier[FindMinPosition(
      this._bFireRateMultiplier)] = buff;
  }

  public void AddSpiralBullets(int value, float duration) {
    var buff = new Buff<int> {
      EndLifeTime = (float)this._time + duration,
      Value = value
    };
    this._bSpiralBullets[FindMinPosition(
      this._bSpiralBullets)] = buff;
  }

  public void AddSpiralBulletsPerCircle(int value, float duration) {
    var buff = new Buff<int> {
      EndLifeTime = (float)this._time + duration,
      Value = value
    };
    this._bSpiralBulletsPerCircle[FindMinPosition(
      this._bSpiralBulletsPerCircle)] = buff;
  }


  private static int FindMinPosition<T>(InlineArray3<Buff<T>> buff) {
    var (a, b, c) = (buff[0].EndLifeTime, buff[1].EndLifeTime, buff[2].EndLifeTime);
    if (a <= b && a <= c) return 0;
    if (b <= a && b <= c) return 1;
    return 2;
  }
}
