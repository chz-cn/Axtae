
using Character.Player;
using Godot;
using Scene.P1;

namespace Scence.P1;

public sealed partial class Main : Node2D {
  public override void _EnterTree() {
    PackedScene player_scene = ResourceLoader
      .Load<PackedScene>("res://character/player/player.tscn");
    Player player = player_scene.Instantiate<Player>();

    Camera camera = new() { TargetToFollow = player };

    this.AddChild(camera);
    this.AddChild(player);
  }
}
