
using System;
using Game.Character.Player;
using Godot;

namespace Game.Config.Pickup;

#pragma warning disable S3453 // Classes should not have only "private"
// constructors
public sealed class Empty : IPickup<Player> {
  public float Duration => 0;

  public string TexturePath => string.Empty;

  public Shape2D Shape => throw new NotSupportedException();

  public static readonly Empty Instance = new();

  private Empty() { }

  public void ApplyTo(Player target) {
    // Method intentionally left empty.
  }
}
#pragma warning restore S3453 // Classes should not have only "private"
// constructors
