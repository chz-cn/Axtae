
namespace Game.Config.Character.Player;

public struct Player() {
  public enum Form : byte { Normal, Armed }
  public enum ShotPattern : byte { Normal, Spiral }

  public float Speed { readonly get; set; } = 48f;
  public float ShootDelay { readonly get; set; } = .4f;
  public float SpiralShootDelay { readonly get; set; } = .01f;

  public ushort SpiralBullets { readonly get; set; } = 64;
  public byte SpiralBulletsPerCircle { readonly get; set; } = 16;

  public float SpeedMultiplier { readonly get; set; } = 1;
  public float FireRateMultiplier { readonly get; set; } = 1;

  public Form FormMode { readonly get; set; } = Form.Normal;
  public ShotPattern ShotPatternMode { readonly get; set; } = ShotPattern.Normal;
}
