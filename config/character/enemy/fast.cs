
namespace Config.Character.Enemy;

public readonly struct Fast() : IPickup<Player.Player> {
  public float Duration { readonly get; init; } = 5f;

  public Player.Player GetPickup() {
    return new() {
      FireRateMultiplier = 2f,
    };
  }


}
