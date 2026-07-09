
using System;
using System.Runtime.CompilerServices;
using Game.Combat;
using Game.Config;
using Godot;
using static Core.Logger;

using L = Game.Config.Layer;

namespace Game.Character.Enemy.Tic;

public sealed partial class Bom : CharacterBody2D,
  ITakeDamage,
  IDropable {
  public const uint Layer = L.CharacterBody;
  public const uint Mask = L.World | L.CharacterBody | L.CharacterSensor
    | L.Projectile | L.AreaEffect | L.Melee | L.Hazard;

  public const float BlinkTime = 1.5f;
  public const float DamageInterval = 0.5f;
  public static readonly StringName Move = "move";

  public required Player.Player TargetPlayer { get; set; }
  public float Speed { get; init; } = 15;

  private readonly uint _mask = (uint)Core.Rng.Shared.NextUInt64();
  public uint MaxHealth { get; init; } = 2;
  public uint Health {
    get => field ^ this._mask;
    private set => field = value ^ this._mask;
  }

  public ReadOnlySpan<(IPickup, uint)> DropItems => _drop;

#pragma warning disable S3459 // Unassigned members should be removed
  private static readonly InlineArray4<(IPickup, uint)> _drop;
#pragma warning restore S3459 // Unassigned members should be removed

  private float _damage_timer = 0;
  private bool _touched = false;

  private readonly AnimatedSprite2D _body_sprite = new() {
    Frame = 0,
    Autoplay = Move,
    SpriteFrames = ResourceLoader.Load<SpriteFrames>(Url.Tres.Bom),
    Material = new ShaderMaterial {
      Shader = IBlinkable.Shader
    }
  };

  static Bom() {
    _drop[0] = (Config.Pickup.Rapid.Instance, 30);
    _drop[1] = (Config.Pickup.Speed.Instance, 30);
    _drop[2] = (Config.Pickup.Spiral.Instance, 20);
    _drop[3] = (Config.Pickup.Empty.Instance, 20);
  }

  public Bom() {
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

    sprite.AnimationFinished += this.QueueFree;

    this.AddChild(sprite);
    this.AddChild(new CollisionShape2D {
      Shape = new CircleShape2D { Radius = 4 }
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
      Shape = new CircleShape2D { Radius = 8 }
    });
    this.AddChild(area);
  }

  public override void _PhysicsProcess(double delta) {
    if (this.Health is 0) {
      this.SetPhysicsProcess(false);

      StringName name = "bom";

      var sprite = this._body_sprite;
      if (sprite is null) return;
      if (sprite.SpriteFrames.HasAnimation(name)) {
        if (sprite.Animation != name) sprite.Play(name);
      }
      else Log(Level.Warning, "bom animation not found");

      var query = new PhysicsShapeQueryParameters2D {
        Shape = new CircleShape2D { Radius = 30 },
        CollisionMask = L.AreaEffect,
        CollideWithAreas = false,
        CollideWithBodies = true,
        Transform = new(0, this.GlobalPosition),
        Exclude = [this.GetRid()]
      };
      var res = this.GetWorld2D().DirectSpaceState.IntersectShape(query, 16);

      foreach (var item in res) {
        if (item["collider"].Obj is ITakeDamage p)
          p.TakeDamage(2);
      }

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
