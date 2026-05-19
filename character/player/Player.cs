
using Godot;
using Cfg = Config.Character;

namespace Character.Player;

public partial class Player : CharacterBody2D {
  public const float Speed = 30f;
  public const float Offset = 10f;
  public const float ShootDelay = .4f;
  public const float SpiralShootDelay = .1f;

  public const ushort SpiralBullets = 64;
  public const byte SpiralBulletsPerCircle = 16;

  public const float DefaultFireRateMultplier = 1f;
  public const float DefaultSpeedMultplier = 1f;

  // nodes
  private AnimatedSprite2D? _body_sprite;
  private AnimatedSprite2D? _armed_effect_sprite;
  private PackedScene? _bullet_scene;

  private System.Collections.IEnumerator? _spiral_shoot = null;

  private Cfg.FacingDirection _facing = Cfg.FacingDirection.Right;

  // config
  private int _health = 300;

  private float _speed_multplier = 1f;
  private float _fire_rate_multplier = 1f;

  private Cfg.Form _form = Cfg.Form.Normal;
  private Cfg.ShotPattern _shot_pattern = Cfg.ShotPattern.Normal;

  // timers
  private float _shoot_timer = 0f;
  private float _spiral_shoot_timer = 0f;
  private float _spiral_accumulator = 0f;

  private float _speed_time_left = 0f;
  private float _rapid_fire_time_left = 0f;
  private float _form_time_left = 0f;

  private static Cfg.FacingDirection Vector2FacingSuffix(Vector2 input)
    => (Mathf.Abs(input.X) >= Mathf.Abs(input.Y))
      ? (input.X > 0f ? Cfg.FacingDirection.Right : Cfg.FacingDirection.Left)
      : (input.Y > 0f ? Cfg.FacingDirection.Down : Cfg.FacingDirection.Up);

  public override void _Ready() {
    this._body_sprite = this.GetNode<AnimatedSprite2D>("Body");
    this._armed_effect_sprite = this.GetNode<AnimatedSprite2D>("ArmedEffect");
    this._bullet_scene = ResourceLoader
      .Load<PackedScene>("res://combat/projectile/Bullet.tscn");

    if (this._body_sprite == null) {
      GD.PrintErr("Missing Player sprite");
    }
    else {
      this._body_sprite.AnimationFinished += () => {
        if (this._body_sprite != null) {
          this.QueueFree();
        }
      };
    }
    if (this._armed_effect_sprite == null) {
      GD.PrintErr("Missing Player armed effect sprite");
    }
    if (this._bullet_scene == null) {
      GD.PrintErr("Failed to load bullet scene");
    }
  }

  public override void _PhysicsProcess(double delta) {
    Vector2 input = Input.GetVector(
      "move_left",
      "move_right",
      "move_up",
      "move_down");

    if (this._body_sprite == null) { return; }

    this.Velocity = input.Normalized() * Speed;
    this.MoveAndSlide();

    this.UpdateAnimDirection();
    this.UpdateAnimation();

    this.UpdatePickupEffect((float)delta);

    if (this._shoot_timer > 0) { this._shoot_timer -= (float)delta; }

    if (Input.IsActionPressed("shoot") && this._shoot_timer <= 0) {
      this.Shoot();
      this._shoot_timer = ShootDelay / this._fire_rate_multplier;
    }

    if (this._spiral_shoot != null) {
      this._spiral_accumulator += (float)delta;
      ushort steps = (ushort)(this._spiral_accumulator / SpiralShootDelay);
      if (steps > 0) {
        for (ushort i = 0; i < steps; i++) {
          if (!this._spiral_shoot.MoveNext()) {
            this._spiral_shoot = null;
            this._form = Cfg.Form.Normal;
            this.UpdateArmedEffect();
            break;
          }
        }
        this._spiral_accumulator -= steps * SpiralShootDelay;
      }
    }
  }

  public override void _Input(InputEvent @event) {
    if (@event.IsActionPressed("shoot")) {
      this._spiral_shoot = this.SpiralShoot(
        SpiralBullets,
        SpiralBulletsPerCircle);
      this._form = Cfg.Form.Armed;
      this.UpdateArmedEffect();
    }
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

      if (this._body_sprite.Animation != name) {
        this._body_sprite.Play(name);
      }
    }
  }

  public bool ApplyPickup(Config.Pickup config) {
    if (config == null) return false;

    System.Func<float, float, bool> approx_equal = (x, y)
      => System.Math.Abs(x - y) < 0.01f;

    bool applied = false;
    float duration = System.Math.Max(config.Duration, 0f);
    bool has_form_override = config.FormMode != Cfg.Form.Normal
      || config.ShotPattern != Cfg.ShotPattern.Normal;
    bool has_fire_rate_override
      = !approx_equal(config.FireRateMultplier, DefaultFireRateMultplier);

    if (!approx_equal(config.MoveSpeedMultplier, DefaultSpeedMultplier)) {
      this._speed_multplier = config.MoveSpeedMultplier;
      this._speed_time_left = duration;
      applied = true;
    }

    if (has_fire_rate_override && !has_form_override) {
      this._fire_rate_multplier = config.FireRateMultplier;
      this._rapid_fire_time_left = duration;
      applied = true;
    }

    if (has_form_override) {
      this._form = config.FormMode;
      this.UpdateArmedEffect();
      this._shot_pattern = config.ShotPattern;
      this._fire_rate_multplier = has_fire_rate_override
        ? config.FireRateMultplier : 1f;
      this._form_time_left = duration;
      applied = true;
    }

    return applied;
  }

  private void UpdateAnimDirection() {
    Vector2 mousePos = this.GetGlobalMousePosition();
    Vector2 direction = mousePos - this.GlobalPosition;

    if (direction.LengthSquared() <= 0.01f) {
      return;
    }

    this._facing = Vector2FacingSuffix(direction);
  }

  private void UpdateAnimation() {
    if (this._body_sprite == null) { return; }

    StringName name = Cfg.Form2Prefix(this._form)
      + Cfg.Facing2Suffix(this._facing);
    if (!this._body_sprite.SpriteFrames.HasAnimation(name)) {
      GD.PushWarning(name + " not found");
      return;
    }

    if (this._body_sprite.Animation != name) { this._body_sprite.Play(name); }
  }

  private void UpdateArmedEffect() {
    if (this._armed_effect_sprite == null) { return; }

    if (this._form != Cfg.Form.Armed) {
      this._armed_effect_sprite.Visible = false;
      if (this._armed_effect_sprite.IsPlaying()) {
        this._armed_effect_sprite.Stop();
      }
      return;
    }

    this._armed_effect_sprite.Visible = true;
    if (this._armed_effect_sprite.IsPlaying()) { return; }

    StringName effect = "default";
    if (this._armed_effect_sprite.SpriteFrames.HasAnimation(effect)) {
      this._armed_effect_sprite.Play(effect);
    }
  }

  private void UpdatePickupEffect(float delta) {
    if (this._speed_time_left > 0f) {
      this._speed_time_left -= delta;
      if (this._speed_time_left <= 0f)
        this._speed_multplier = DefaultSpeedMultplier;
    }

    if (this._rapid_fire_time_left > 0f) {
      this._rapid_fire_time_left -= delta;
      if (this._rapid_fire_time_left <= 0)
        this._fire_rate_multplier = DefaultSpeedMultplier;
    }

    if (this._form_time_left > 0f) {
      this._form_time_left -= delta;
      if (this._form_time_left <= 0) {
        this._form = Cfg.Form.Normal;
        this._shot_pattern = Cfg.ShotPattern.Normal;
        this._fire_rate_multplier = DefaultFireRateMultplier;
      }
    }
  }

  private Vector2 GetShootDirection()
    => this._facing switch {
      Cfg.FacingDirection.Right => Vector2.Right,
      Cfg.FacingDirection.Left => Vector2.Left,
      Cfg.FacingDirection.Up => Vector2.Up,
      Cfg.FacingDirection.Down => Vector2.Down,
      _ => Vector2.Right
    };

  private void Shoot() {
    if (this._bullet_scene == null) { return; }

    Bullet bullet = this._bullet_scene.Instantiate<Bullet>();

    bullet.GlobalPosition = this.GlobalPosition
      + this.GetShootDirection() * Offset;
    bullet.Setup(this.GetShootDirection());

    this.GetTree().Root.AddChild(bullet);
    bullet.PlayAudio();
  }

  private System.Collections.IEnumerator SpiralShoot(
    ushort total = 64,
    byte shots_per_circle = 16) {
    if (this._bullet_scene == null || shots_per_circle < 1 || total < 2) {
      yield break;
    }

    float angle_step = 360f / shots_per_circle;
    total /= 2;

    while (total > 0) {
      ushort bullets_this_circle = System.Math.Min(shots_per_circle, total);

      for (uint i = 0; i < bullets_this_circle; i++) {
        float deg = i * angle_step;
        Vector2 direction_f = Vector2.FromAngle(Mathf.DegToRad(deg));
        Vector2 direction_b = -direction_f;

        Bullet forward = this._bullet_scene.Instantiate<Bullet>();
        Bullet backward = this._bullet_scene.Instantiate<Bullet>();

        forward.GlobalPosition = this.GlobalPosition + direction_f * Offset;
        backward.GlobalPosition = this.GlobalPosition + direction_b * Offset;

        forward.Setup(direction_f);
        backward.Setup(direction_b);

        this.GetTree().Root.AddChild(forward);
        this.GetTree().Root.AddChild(backward);
        forward.PlayAudio();
        yield return null;
      }

      total -= bullets_this_circle;
    }
  }
}
