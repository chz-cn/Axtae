
namespace Config.Character.Player;

public record class Player {
  public float Speed { get; init; } = 30f;
  public float ShootDelay { get; init; } = .4f;
  public float SpiralShootDelay { get; init; } = .1f;

  public ushort SpiralBullets { get; init; } = 64;
  public byte SpiralBulletsPerCircle { get; init; } = 16;

  public float SpeedMultiplier { get; init; } = 1;
  public float FireRateMultiplier { get; init; } = 1;

  public enum Form : byte { Normal, Armed }
  public Form FormMode = Form.Normal;

  public enum ShotPattern : byte { Normal, Spiral }
  public ShotPattern ShotPatternMode = ShotPattern.Normal;
}
