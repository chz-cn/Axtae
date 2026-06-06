
using Godot;
using Bullet = Combat.Projectile.Bullet;
using Cfg = Config.Character.Player.Player;

namespace Character.Player;

public partial class Player : CharacterBody2D {
  public const float ShotOffset = 10f;

  public const float DefaultFireRateMultiplier = 1f;
  public const float DefaultSpeedMultiplier = 1f;

  private Cfg _config = new();

  // nodes
  private AnimatedSprite2D? _body_sprite;
  private AnimatedSprite2D? _armed_effect_sprite;
  private PackedScene? _bullet_scene;

  private PhysicsDirectSpaceState2D? _state;
  private PhysicsRayQueryParameters2D? _ray_query;

  private System.Collections.IEnumerator? _spiral_shoot = null;

  public enum FacingDirection : byte { Right, Left, Up, Down }
  private FacingDirection _facing = FacingDirection.Right;

  // data
  private int _health;

  // timers
  private float _shoot_timer = 0f;
  private float _spiral_accumulator = 0f;

  private float _speed_time_left = 0f;
  private float _fire_rate_time_left = 0f;
  private float _form_time_left = 0f;

  public static StringName Form2Prefix(Cfg.Form form)
    => form switch {
      Cfg.Form.Normal => "n_",
      Cfg.Form.Armed => "armed_",
      _ => "n_"
    };

  public static StringName Facing2Suffix(FacingDirection facing)
    => facing switch {
      FacingDirection.Right => "right",
      FacingDirection.Left => "left",
      FacingDirection.Up => "up",
      FacingDirection.Down => "down",
      _ => "right"
    };

  public static FacingDirection Vector2FacingSuffix(Vector2 input)
    => (Mathf.Abs(input.X) >= Mathf.Abs(input.Y))
      ? (input.X > 0f ? FacingDirection.Right : FacingDirection.Left)
      : (input.Y > 0f ? FacingDirection.Down : FacingDirection.Up);

  public override void _Ready() {
    this._body_sprite = this.GetNodeOrNull<AnimatedSprite2D>("Body");
    this._armed_effect_sprite = this.GetNodeOrNull<AnimatedSprite2D>("ArmedEffect");
    this._bullet_scene = ResourceLoader
      .Load<PackedScene>("res://combat/projectile/Bullet.tscn");

    this._state = this.GetWorld2D().DirectSpaceState;
    this._ray_query = new() {
      CollisionMask = Bullet.Mask,
      CollideWithAreas = false,
      CollideWithBodies = true,
      Exclude = [this.GetRid()]
    };

    if (this._body_sprite == null) GD.PrintErr("Missing Player sprite");
    else {
      this._body_sprite.AnimationFinished += () => {
        if (this._body_sprite != null) this.QueueFree();
      };
    }

    if (this._armed_effect_sprite == null)
      GD.PrintErr("Missing Player armed effect sprite");
    if (this._bullet_scene == null)
      GD.PrintErr("Failed to load bullet scene");
  }

  public override void _PhysicsProcess(double delta) {
    Vector2 input = Input.GetVector(
      "move_left",
      "move_right",
      "move_up",
      "move_down");

    if (this._body_sprite == null) return;

    this.Velocity = input.Normalized()
      * this._config.Speed * this._config.SpeedMultiplier;
    this.MoveAndSlide();

    this.UpdateAnimDirection();
    this.UpdateAnim();

    float dt = (float)delta;

    this.UpdatePickupEffect(dt);

    this.UpdateShoot(dt);
    this.UpdateSpiralShoot(dt);
  }

  public void TakeDamage(int x) {
    if (System.Threading.Interlocked.Add(ref this._health, -x) <= 0) {
      this.SetPhysicsProcess(false);
      if (this._body_sprite == null) {
        this.QueueFree();
        return;
      }

      StringName name = "die";
      if (!this._body_sprite.SpriteFrames.HasAnimation(name)) {
        GD.PushWarning("die animation not found");
        return;
      }

      if (this._body_sprite.Animation != name) this._body_sprite.Play(name);
    }
  }

  public bool ApplyConfig(in Cfg config, float duration = 5f) {
    if (config == null || duration <= 0f) return false;

    bool change_speed_multiplier = false;
    bool change_fire_rate_multiplier = false;
    bool change_form = false;
    bool change_shotpattern = false;

    if (!Mathf.IsZeroApprox(
      config.SpeedMultiplier - DefaultSpeedMultiplier)) {
      this._speed_time_left = duration;
      change_speed_multiplier = true;
    }

    if (!Mathf.IsZeroApprox(
      config.FireRateMultiplier - DefaultFireRateMultiplier)) {
      this._fire_rate_time_left = duration;
      change_fire_rate_multiplier = true;
    }

    if (config.FormMode != Cfg.Form.Normal) {
      this._form_time_left = duration;

      this.UpdateArmedEffect();
      change_form = true;
    }

    if (config.ShotPatternMode == Cfg.ShotPattern.Spiral) {
      this._spiral_shoot = this.SpiralShoot(
        this._config.SpiralBullets,
        this._config.SpiralBulletsPerCircle);
      change_shotpattern = true;
    }

    bool changed = change_form || change_speed_multiplier
      || change_fire_rate_multiplier || change_shotpattern;

    if (changed)
      this._config = this._config with {
        SpeedMultiplier = change_speed_multiplier
          ? config.SpeedMultiplier : this._config.SpeedMultiplier,
        FireRateMultiplier = change_fire_rate_multiplier
          ? config.FireRateMultiplier : this._config.FireRateMultiplier,
        FormMode = change_form ? config.FormMode : this._config.FormMode,
        ShotPatternMode = change_shotpattern
          ? config.ShotPatternMode : this._config.ShotPatternMode
      };

    return changed;
  }

  private void UpdateAnimDirection() {
    Vector2 mousePos = this.GetGlobalMousePosition();
    Vector2 direction = mousePos - this.GlobalPosition;

    if (direction.LengthSquared() <= 0.01f) return;

    this._facing = Vector2FacingSuffix(direction);
  }

  private void UpdateAnim() {
    if (this._body_sprite == null) return;

    StringName name = Form2Prefix(this._config.FormMode)
      + Facing2Suffix(this._facing);
    if (!this._body_sprite.SpriteFrames.HasAnimation(name)) {
      GD.PushWarning(name, " not found");
      return;
    }

    if (this._body_sprite.Animation != name) this._body_sprite.Play(name);
  }

  private void UpdateArmedEffect() {
    if (this._armed_effect_sprite == null) return;

    if (this._config.FormMode != Cfg.Form.Armed) {
      this._armed_effect_sprite.Visible = false;
      if (this._armed_effect_sprite.IsPlaying())
        this._armed_effect_sprite.Stop();

      return;
    }

    this._armed_effect_sprite.Visible = true;
    if (this._armed_effect_sprite.IsPlaying()) return;

    StringName effect = "default";
    if (this._armed_effect_sprite.SpriteFrames.HasAnimation(effect))
      this._armed_effect_sprite.Play(effect);
  }

  private void UpdatePickupEffect(float delta) {
    bool change_speed_multiplier = false;
    bool change_fire_rate_multiplier = false;
    bool change_form = false;

    if (this._speed_time_left > 0f) {
      this._speed_time_left -= delta;
      if (this._speed_time_left <= 0f)
        change_speed_multiplier = true;
    }

    if (this._fire_rate_time_left > 0f) {
      this._fire_rate_time_left -= delta;
      if (this._fire_rate_time_left <= 0)
        change_fire_rate_multiplier = true;
    }

    if (this._form_time_left > 0f) {
      this._form_time_left -= delta;
      if (this._form_time_left <= 0)
        change_form = true;
    }

    if (change_speed_multiplier
      || change_fire_rate_multiplier
      || change_form) {
      Cfg normol = new();
      this._config = this._config with {
        SpeedMultiplier = change_speed_multiplier
          ? normol.SpeedMultiplier : this._config.SpeedMultiplier,
        FireRateMultiplier = change_fire_rate_multiplier
          ? normol.FireRateMultiplier : this._config.FireRateMultiplier,
        FormMode = change_form ? normol.FormMode : this._config.FormMode,
      };
    }

    if (change_form) this.UpdateArmedEffect();
  }

  private void UpdateShoot(float delta) {
    if (this._shoot_timer > 0) this._shoot_timer -= delta;

    if (Input.IsActionPressed("shoot") && this._shoot_timer <= 0) {
      this.Shoot();
      this._shoot_timer = this._config.ShootDelay / this._config.FireRateMultiplier;
    }
  }

  private void UpdateSpiralShoot(float delta) {
    if (this._spiral_shoot == null) return;

    this._spiral_accumulator += delta;
    ushort steps =
      (ushort)(this._spiral_accumulator / this._config.SpiralShootDelay);

    if (steps == 0) return;

    for (ushort i = 0; i < steps; i++) {
      if (this._spiral_shoot.MoveNext()) continue;

      this._spiral_shoot = null;
      this._config = this._config with {
        ShotPatternMode = Cfg.ShotPattern.Normal
      };
      this._spiral_accumulator = 0f;
      return;
    }
    this._spiral_accumulator -= steps * this._config.SpiralShootDelay;
  }

  private Vector2 GetShootDirection()
    => this._facing switch {
      FacingDirection.Right => Vector2.Right,
      FacingDirection.Left => Vector2.Left,
      FacingDirection.Up => Vector2.Up,
      FacingDirection.Down => Vector2.Down,
      _ => Vector2.Right
    };

  private bool? WillHit(Vector2 direction, Vector2 from, float offset) {
    if (this._ray_query == null
      || this._state == null
      || direction == Vector2.Zero
      || offset <= 0f) return null;

    Vector2 to = from + direction.Normalized() * offset;
    this._ray_query.From = from;
    this._ray_query.To = to;

    return this._state.IntersectRay(this._ray_query).Count > 0;
  }

  private void Shoot() {
    if (this._bullet_scene == null) return;
    bool? hit = this.WillHit(this.GetShootDirection(),
      this.GlobalPosition,
      ShotOffset);
    if (hit == null || hit == true) return;

    Bullet bullet = this._bullet_scene.Instantiate<Bullet>();
    bullet.GlobalPosition
      = this.GlobalPosition + this.GetShootDirection() * ShotOffset;

    bullet.Setup(this.GetShootDirection());
    this.GetTree().Root.AddChild(bullet);
    bullet.PlayAudio();
  }

  private System.Collections.IEnumerator SpiralShoot(
    ushort total = 64,
    byte shots_per_circle = 16) {
    if (this._bullet_scene == null || shots_per_circle < 1 || total < 2)
      yield break;

    float angle_step = 360f / shots_per_circle;
    total /= 2;

    while (total > 0) {
      ushort bullets_this_circle = System.Math.Min(shots_per_circle, total);

      for (uint i = 0; i < bullets_this_circle; i++) {
        float deg = i * angle_step;
        Vector2 direction_f = Vector2.FromAngle(Mathf.DegToRad(deg));
        Vector2 direction_b = -direction_f;

        if (this.WillHit(direction_f,
          this.GlobalPosition,
          ShotOffset) == false) {
          Bullet forward = this._bullet_scene.Instantiate<Bullet>();
          forward.GlobalPosition
            = this.GlobalPosition + direction_f * ShotOffset;

          forward.Setup(direction_f);
          this.GetTree().Root.AddChild(forward);
          forward.PlayAudio();
        }

        if (this.WillHit(direction_b,
          this.GlobalPosition,
          ShotOffset) == false) {
          Bullet backward = this._bullet_scene.Instantiate<Bullet>();

          backward.GlobalPosition
            = this.GlobalPosition + direction_b * ShotOffset;

          backward.Setup(direction_b);
          this.GetTree().Root.AddChild(backward);
        }
        yield return null;
      }

      total -= bullets_this_circle;
    }
  }
}
