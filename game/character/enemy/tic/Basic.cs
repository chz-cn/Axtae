
using System;
using System.Runtime.CompilerServices;
using Game.Combat;
using Game.Config;
using Godot;
using static Core.Logger;

using L = Game.Config.Layer;

namespace Game.Character.Enemy.Tic;

public sealed partial class Basic : CharacterBody2D,
  ITakeDamage,
  IDropable {
  public const uint Layer = L.CharacterBody;
  public const uint Mask = L.World | L.CharacterBody | L.CharacterSensor
    | L.Projectile | L.AreaEffect | L.Melee | L.Hazard;

  public const float BlinkTime = 1.5f;
  public const float DamageInterval = 0.5f;
  public static readonly StringName Move = "move";

  public required Player.Player TargetPlayer { get; set; }
  public float Speed { get; init; } = 20;

  private readonly uint _mask = (uint)Core.Rng.Shared.NextUInt64();
  public uint MaxHealth { get; init; } = 3;
  public uint Health {
    get => field ^ this._mask;
    private set => field = value ^ this._mask;
  }

  public ReadOnlySpan<(IPickup, uint)> DropItems => _drop;

#pragma warning disable S3459 // Unassigned members should be removed
  private static readonly InlineArray3<(IPickup, uint)> _drop;
#pragma warning restore S3459 // Unassigned members should be removed

  private float _damage_timer = 0;
  private bool _touched = false;

  private readonly AnimatedSprite2D _body_sprite = new() {
    Frame = 0,
    Autoplay = Move,
    SpriteFrames = ResourceLoader.Load<SpriteFrames>(Url.Tres.Basic),
    Material = new ShaderMaterial {
      Shader = IBlinkable.Shader
    }
  };

  static Basic() {
    _drop[0] = (Config.Pickup.Rapid.Instance, 10);
    _drop[1] = (Config.Pickup.Speed.Instance, 10);
    _drop[2] = (Config.Pickup.Empty.Instance, 80);
  }

  public Basic() {
    this.CollisionLayer = Layer;
    this.CollisionMask = Mask;
  }

  public override void _EnterTree() {
    if (this.MaxHealth is 0) {
      this.QueueFree();
      return;
    }
    this.Health = this.MaxHealth;

    var sprite = this._body_sprite;

    sprite.AnimationFinished += () => {
      var item = IDropable.GetDrop(_drop);
      if (item is not null) {
        item.GlobalPosition = this.GlobalPosition;
        this.GetTree().Root.AddChild(item);
      }
      this.QueueFree();
    };

    this.AddChild(sprite);
    this.AddChild(new CollisionShape2D {
      Shape = new CircleShape2D { Radius = 6 }
    });

    Area2D area = new() {
      CollisionLayer = Layer,
      CollisionMask = L.CharacterBody
    };

    area.BodyEntered += (node) => {
      if (this.Health is not 0 && node is Player.Player) {
        this._touched = true;
        this._damage_timer = 0;
      }
    };

    area.BodyExited += (node) => {
      if (node is Player.Player) this._touched = false;
    };

    area.AddChild(new CollisionShape2D {
      Shape = new CircleShape2D { Radius = 6 }
    });
    this.AddChild(area);
  }

  public override void _PhysicsProcess(double delta) {
    if (this.Health is 0) {
      this.SetPhysicsProcess(false);

      StringName name = "die";

      var sprite = this._body_sprite;
      if (sprite is null) return;
      if (sprite.SpriteFrames.HasAnimation(name)) {
        if (sprite.Animation != name) sprite.Play(name);
      }
      else Log(Level.Warning, "die animation not found");

      return;
    }

    var dt = (float)delta;
    this.UpdateTouchDamage(dt);

    if (!IsInstanceValid(this.TargetPlayer)) {
      this._touched = false;
      this.Velocity = Vector2.Zero;
      this.MoveAndSlide();
      return;
    }

    var move_direction = this.GlobalPosition.DirectionTo(
      this.TargetPlayer.GlobalPosition);
    this.UpdateFacingDirection(move_direction);
    this.Velocity = move_direction * this.Speed;
    this.MoveAndSlide();
  }

  public void TakeDamage(uint damage) {
    uint val = this.Health;
    uint x = val - damage;
    this.Health = val > damage ? x : 0;
  }

  public void UpdateFacingDirection(Vector2 direction) {
    if (Mathf.IsZeroApprox(direction.X)) return;

    this._body_sprite.FlipH = direction.X < 0;
  }

  private void UpdateTouchDamage(float dt) {
    this._damage_timer -= dt;

    if (!this._touched
      || !IsInstanceValid(this.TargetPlayer)
      || this._damage_timer > 0) return;

    this.TargetPlayer.TakeDamage(1);
    this._damage_timer = DamageInterval;
  }
}
