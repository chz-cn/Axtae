
using Game.Character.Player;
using Godot;

namespace Game.Scene.P1;

public sealed partial class Main : Node2D {
  private readonly Node2D _enemy_continer = new();
  private readonly Player _player;

  public Main() {
    PackedScene player_scene = ResourceLoader
      .Load<PackedScene>(Url.Tscn.Player);

    this._player = player_scene.Instantiate<Player>();
  }

  public override void _EnterTree() {
    var player = this._player;

    Camera camera = new() { TargetToFollow = player };

    this.AddChild(camera);
    this.AddChild(player);
    this.AddChild(this._enemy_continer);
  }
}
