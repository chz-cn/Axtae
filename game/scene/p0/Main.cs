
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Core;
using Core.Random;
using Game.Character.Enemy.Tic;
using Game.Character.Player;
using Godot;

namespace Game.Scene.P0;

public sealed partial class Main : Node2D {
  public const float MinSpawnInterval = .6f;
  public const uint MaxAliveEnemy = 12;

  public readonly Player Player;
  private readonly Node2D _enemy_continer = new();
  private InlineArray3<Vector2> _enemy_spawn = new();

  private Xoroshiro128PlusPlus _rand = new(Rng.Shared.NextUInt64());

  private float _spawn_interval = 1.5f;
  private uint _enemy_count = 0;
  private float _enemy_timer = 0;

  private double _timer = 0;

  public Main() {
    PackedScene player_scene = ResourceLoader
      .Load<PackedScene>(Url.Tscn.Player);

    var player = player_scene.Instantiate<Player>();
    player.GlobalPosition = this.GlobalPosition + new Vector2(100, 100);

    this.Player = player;
  }

  public override void _EnterTree() {
    var player = this.Player;

    Camera camera = new() { TargetToFollow = player };

    this.AddChild(camera);
    this.AddChild(player);
    this.AddChild(this._enemy_continer);
  }

  public override void _Ready() {
    var coord = this.GetNodeOrNull<Node2D>("Coord");
    this._enemy_spawn[0] = coord.GetNodeOrNull<Marker2D>("Spawn0")?
      .Position ?? Vector2.Zero;
    this._enemy_spawn[1] = coord.GetNodeOrNull<Marker2D>("Spawn1")?
      .Position ?? Vector2.Zero;
    this._enemy_spawn[2] = coord.GetNodeOrNull<Marker2D>("Spawn2")?
      .Position ?? Vector2.Zero;
  }

  public override void _PhysicsProcess(double delta) {
    this._timer += delta;

    float t = Math.Clamp((float)(this._timer / 60.0), 0f, 1f);
    this._spawn_interval = Mathf.Lerp(1.5f, MinSpawnInterval, t);

    if (this._timer >= this._enemy_timer) {
      if (this._enemy_count >= MaxAliveEnemy) return;

      this.SpawnEnemy();
      Interlocked.Increment(ref this._enemy_count);
      this._enemy_timer += this._spawn_interval;
    }
  }

  private void SpawnEnemy() {
    Node2D enemy = this._rand.NextUInt64(4) switch {
      0 => new Basic { TargetPlayer = this.Player },
      1 => new Fast { TargetPlayer = this.Player },
      2 => new Bom { TargetPlayer = this.Player },
      3 => new Shelled { TargetPlayer = this.Player },
      _ => new Basic { TargetPlayer = this.Player }
    };

    enemy.Position = this._enemy_spawn[(int)this._rand.NextUInt64(3)];
    enemy.TreeExiting += () => Interlocked.Decrement(ref this._enemy_count);

    this._enemy_continer.AddChild(enemy);
  }
}
