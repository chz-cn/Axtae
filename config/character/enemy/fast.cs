
using Godot;

namespace Config.Character.Enemy;

public class Fast : IPickup<Player.Player> {
  public float Duration { get; init; } = 5f;

  public Player.Player GetPickup() {
    return new() {
      FireRateMultiplier = 2f,
    };
  }


}
