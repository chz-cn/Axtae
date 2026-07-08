
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
  IBlinkable,
  IDropable {
  public const uint Layer = L.CharacterBody;
  public const uint Mask = L.World | L.CharacterBody | L.CharacterSensor
    | L.Projectile | L.AreaEffect | L.Melee | L.Hazard;

  public const float BlinkTime = 1.5f;
  public const float DamageInterval = 0.5f;
  public static readonly StringName Move = "move";

  public required Player.Player TargetPlayer { get; set; }
  public float Speed { get; init; } = 30;

  private readonly uint _mask = (uint)Core.Rng.Shared.NextUInt64();
  public uint MaxHealth { get; init; } = 4;
  public uint Health {
    get => field ^ this._mask;
    private set => field = value ^ this._mask;
  }

  public ReadOnlySpan<(IPickup, uint)> DropItems => [];

  #region IBlinkable

  public float BlinkSpeed {
    get; private set {
      if (value is > .1f and < 30) {
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter(Pickup.Scene.Blink.BlinkSpeed, value);
        field = value;
      }
    }
  }

  public float HiddenRatio {
    get; private set {
      if (value is > 0f and < 1f) {
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter(Pickup.Scene.Blink.HiddenRatio, value);
        field = value;
      }
    }
  }

  public bool Blink {
    get; private set {
      if (field != value) {
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter(Pickup.Scene.Blink.blink, value);
        field = value;
      }
    }
  }

  #endregion

  private float _damage_timer = 0;
  private float _blink_timer = 0;
  private bool _touch_palyer = false;

  private readonly AnimatedSprite2D _body_sprite = new() {
    Frame = 0,
    Autoplay = Move,
    SpriteFrames = ResourceLoader.Load<SpriteFrames>(Url.Tres.Bom),
    Material = new ShaderMaterial {
      Shader = IBlinkable.Shader
    }
  };

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
      if (this.Health is not 0 && node is Player.Player player) {
        this._touch_palyer = true;
        this._damage_timer = 0;
        player.TakeDamage(1);
      }
    };
    area.BodyExited += (node) => {
      if (node is Player.Player) this._touch_palyer = false;
    };

    area.AddChild(new CollisionShape2D {
      Shape = new CircleShape2D { Radius = 6 }
    });
    this.AddChild(area);
  }

  public override void _PhysicsProcess(double delta) {
    if (this.Health is 0) {
      this.SetPhysicsProcess(false);

      StringName name = "bom";
      if (this._body_sprite is null) return;
      if (this._body_sprite.SpriteFrames.HasAnimation(name)) {
        if (this._body_sprite.Animation != name) this._body_sprite.Play(name);
      }
      else Log(Level.Warning, "bom animation not found");

      var query = new PhysicsShapeQueryParameters2D {
        Shape = new CircleShape2D { Radius = 8 },
        CollisionMask = L.CharacterBody,
        CollideWithAreas = false,
        CollideWithBodies = true,
        Transform = new(0, this.GlobalPosition),
        Exclude = [this.GetRid()]
      };
      var res = this.GetWorld2D().DirectSpaceState.IntersectShape(query, 16);

      foreach (var item in res) {
        if (item["collider"].Obj is ITakeDamage p)
          p.TakeDamage(1);
      }

      return;
    }

    var dt = (float)delta;
    this.UpdateBlink(dt);
    this.UpdateTouchDamage(dt);

    if (!IsInstanceValid(this.TargetPlayer)) {
      this._touch_palyer = false;
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
    var h = this.Health;
    if (h > damage) {
      this.Health -= damage;
      this.Blink = true;
      this._blink_timer = BlinkTime;
    }
    else if (h > 0) this.Health = 0;
  }

  public void UpdateFacingDirection(Vector2 direction) {
    if (Mathf.IsZeroApprox(direction.X)) return;

    this._body_sprite.FlipH = direction.X < 0;
  }

  private void UpdateTouchDamage(float dt) {
    this._damage_timer -= dt;

    if (!this._touch_palyer
      || !IsInstanceValid(this.TargetPlayer)
      || this._damage_timer > 0) return;

    this.TargetPlayer.TakeDamage(1);
    this._damage_timer = DamageInterval;
  }

  private void UpdateBlink(float dt) {
    if (this._blink_timer <= 0) return;

    this._blink_timer -= dt;

    if (this._blink_timer <= 0) {
      this.Blink = false;
      this._blink_timer = 0;
    }
  }
}
