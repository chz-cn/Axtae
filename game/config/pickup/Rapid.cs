
using Game.Character.Player;
using Godot;

namespace Game.Config.Pickup;

public sealed class Rapid : IPickup<Player> {
  public float Duration { get; init; } = 5f;

  public string TexturePath => "res://game/pickup/atlas/rapid.tres";

  public Shape2D Shape => new CircleShape2D { Radius = 6 };

  public static readonly Rapid Instance = new();

  public void ApplyTo(Player target) {
    if (target is null) return;

    target.Config.AddFireRateMultiplier(2f, this.Duration);
  }
}
