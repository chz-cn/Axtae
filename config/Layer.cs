
namespace Config;

public static class Layer {
  public const uint World = 1;
  public const uint Player = 1 << 1;
  public const uint EnemyBody = 1 << 2;
  public const uint EnemySensor = 1 << 3;
  public const uint Pickup = 1 << 4;
  public const uint Projectile = 1 << 5;
  public const uint AreaEffect = 1 << 6;
  public const uint Melee = 1 << 7;
  public const uint Hazard = 1 << 8;
}
