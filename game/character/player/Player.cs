
using Game.Scene.P1;
using Godot;
using static Core.Logger;

using Bullet = Game.Combat.Projectile.Bullet;
using Cfg = Game.Config.Character.Player.Player;

namespace Game.Character.Player;

public sealed partial class Player : CharacterBody2D,
  Combat.IBasicTakeDamage,
  ICameraFollowable {
  public const float ShotOffset = 10f;

  public const float DefaultFireRateMultiplier = 1f;
  public const float DefaultSpeedMultiplier = 1f;

  private Cfg _config = new();
  public Cfg Config => this._config;

  #region node

  private readonly PackedScene _bullet_scene = ResourceLoader
    .Load<PackedScene>(Url.Tscn.Bullet);

  private AnimatedSprite2D? _body_sprite;
  private AnimatedSprite2D? _armed_effect_sprite;

  private PhysicsDirectSpaceState2D? _state;
  private PhysicsRayQueryParameters2D? _ray_query;

  #endregion

  private System.Collections.IEnumerator? _spiral_shoot = null;

  public enum FacingDirection : byte { Right, Left, Up, Down }
  private FacingDirection _facing = FacingDirection.Right;

  // timers
  private float _shoot_timer = 0f;
  private float _spiral_accumulator = 0f;

  // on exit tree
  public event System.Action OnExit {
    add => this.TreeExited += value;
    remove => this.TreeExited -= value;
  }

  private readonly uint _mask = (uint)Core.Rng.Shared.NextUInt64();
  public uint MaxHealth { get; init; } = 4;
  public uint Health {
    get => field ^ this._mask;
    private set => field = value ^ this._mask;
  }

  #region static

  public static string Form2Prefix(Cfg.Form form)
    => form switch {
      Cfg.Form.Normal => "n_",
      Cfg.Form.Armed => "armed_",
      _ => "n_"
    };

  public static string Facing2Suffix(FacingDirection facing)
    => facing switch {
      FacingDirection.Right => "right",
      FacingDirection.Left => "left",
      FacingDirection.Up => "up",
      FacingDirection.Down => "down",
      _ => "right"
    };

#pragma warning disable S3358 // Ternary operators should not be nested
  public static FacingDirection Vector2FacingSuffix(Vector2 input)
    => (Mathf.Abs(input.X) >= Mathf.Abs(input.Y))
      ? (input.X > 0f ? FacingDirection.Right : FacingDirection.Left)
      : (input.Y > 0f ? FacingDirection.Down : FacingDirection.Up);
#pragma warning restore S3358 // Ternary operators should not be nested

  #endregion

  public Player() => this.MotionMode = MotionModeEnum.Floating;

  #region Godot Lifecycle Overrides

  public override void _EnterTree() {
    if (this.MaxHealth is 0) {
      this.QueueFree();
      return;
    }
    this.Health = this.MaxHealth;

    this._state = this.GetWorld2D().DirectSpaceState;
    this._ray_query = new() {
      CollisionMask = Bullet.Mask,
      CollideWithAreas = false,
      CollideWithBodies = true,
      Exclude = [this.GetRid()]
    };

    if (this._bullet_scene is null)
      Log(Level.Warning, "Failed to load bullet scene");
  }

  public override void _ExitTree() => Log(Level.Error, "flush");

  public override void _Ready() {
    this._body_sprite = this.GetNodeOrNull<AnimatedSprite2D>("Body");
    this._armed_effect_sprite = this.GetNodeOrNull<AnimatedSprite2D>("ArmedEffect");

    if (this._body_sprite is null) {
      Log(Level.Warning, "Missing Player sprite");
      this.QueueFree();
    }
    else this._body_sprite.AnimationFinished += this.QueueFree;

    if (this._armed_effect_sprite is null)
      Log(Level.Warning, "Missing Player armed effect sprite");
  }

  public override void _PhysicsProcess(double delta) {
    if (this.Health is 0) {
      this.SetPhysicsProcess(false);

      StringName name = "die";
      if (this._body_sprite is null) return;
      if (this._body_sprite.SpriteFrames.HasAnimation(name)) {
        if (this._body_sprite.Animation != name) this._body_sprite.Play(name);
      }
      else Log(Level.Warning, "die animation not found");

      return;
    }

    Vector2 input = Input.GetVector(
      Game.Config.InputMap.MoveLeft,
      Game.Config.InputMap.MoveRight,
      Game.Config.InputMap.MoveUp,
      Game.Config.InputMap.MoveDown);

    this.Velocity = input.Normalized()
      * this._config.MoveSpeed * this._config.SpeedMultiplier;
    this.MoveAndSlide();

    this.Update((float)delta);
  }

  #endregion

  public void TakeDamage(uint damage) {
    uint val = this.Health;
    uint x = val - damage;
    this.Health = val > damage ? x : 0;
  }

  public void SpiralShoot() {
    this._config.ShotPatternMode = Cfg.ShotPattern.Spiral;
    this._config.FormMode = Cfg.Form.Armed;
    this._spiral_shoot = this.SpiralShoot(this._config.SpiralBullets,
      this._config.SpiralBulletsPerCircle);
  }

  #region update

  private void Update(float delta) {
    this.UpdateAnimDirection();
    this.UpdateAnim();

    float dt = delta;

    this.UpdateShoot(dt);
    this.UpdateSpiralShoot(dt);
  }

  private void UpdateAnimDirection() {
    Vector2 mousePos = this.GetGlobalMousePosition();
    Vector2 direction = mousePos - this.GlobalPosition;

    if (direction.LengthSquared() <= 0.01f) return;

    this._facing = Vector2FacingSuffix(direction);
  }

  private void UpdateAnim() {
    if (this._body_sprite is null) return;

    StringName name = Form2Prefix(this._config.FormMode)
      + Facing2Suffix(this._facing);
    if (!this._body_sprite.SpriteFrames.HasAnimation(name)) {
      GD.PushWarning(name, " not found");
      return;
    }

    if (this._body_sprite.Animation != name) this._body_sprite.Play(name);
  }

  private void UpdateArmedEffect() {
    if (this._armed_effect_sprite is null) return;

    if (this._config.FormMode is not Cfg.Form.Armed) {
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

  private void UpdateShoot(float delta) {
    if (this._shoot_timer > 0) this._shoot_timer -= delta;

    if (Input.IsActionPressed(Game.Config.InputMap.Shoot)
      && this._shoot_timer <= 0) {
      this.Shoot();
      this._shoot_timer = this._config.ShootDelay
        / this._config.FireRateMultiplier;
    }
  }

  private void UpdateSpiralShoot(float delta) {
    if (this._spiral_shoot is null) return;

    this._spiral_accumulator += delta;
    float delay = this._config.SpiralShootDelay;
    ushort steps = (ushort)(this._spiral_accumulator / delay);

    if (steps is 0) return;

    for (ushort i = 0; i < steps; i++) {
      if (this._spiral_shoot.MoveNext()) continue;

      this._spiral_shoot = null;
      this._config.ShotPatternMode = Cfg.ShotPattern.Normal;
      this._config.FormMode = Cfg.Form.Normal;
      this._spiral_accumulator = 0f;
      return;
    }
    this._spiral_accumulator -= steps * delay;
  }

  #endregion

  private bool? WillHit(Vector2 direction, Vector2 from, float offset) {
    if (this._ray_query is null
      || this._state is null
      || direction == Vector2.Zero
      || offset <= 0f) return null;

    Vector2 to = from + (direction.Normalized() * offset);
    this._ray_query.From = from;
    this._ray_query.To = to;

    return this._state.IntersectRay(this._ray_query).Count > 0;
  }

  private void Shoot() {
    Vector2 GetShootDirection()
      => this._facing switch {
        FacingDirection.Right => Vector2.Right,
        FacingDirection.Left => Vector2.Left,
        FacingDirection.Up => Vector2.Up,
        FacingDirection.Down => Vector2.Down,
        _ => Vector2.Right
      };

    if (this._bullet_scene is null) return;
    bool? hit = this.WillHit(GetShootDirection(),
      this.GlobalPosition,
      ShotOffset);
    if (hit is null or true) return;

    Bullet bullet = this._bullet_scene.Instantiate<Bullet>();
    bullet.GlobalPosition
      = this.GlobalPosition + (GetShootDirection() * ShotOffset);

    bullet.Setup(GetShootDirection());
    this.GetTree().Root.AddChild(bullet);
    bullet.PlayAudio();
  }

  private System.Collections.IEnumerator SpiralShoot(
    ushort total,
    byte shots_per_circle) {
    if (this._bullet_scene is null || shots_per_circle < 1 || total < 2)
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
          ShotOffset) is false) {
          Bullet forward = this._bullet_scene.Instantiate<Bullet>();
          forward.GlobalPosition
            = this.GlobalPosition + (direction_f * ShotOffset);

          forward.Setup(direction_f);
          this.GetTree().Root.AddChild(forward);
          forward.PlayAudio();
        }

        if (this.WillHit(direction_b,
          this.GlobalPosition,
          ShotOffset) is false) {
          Bullet backward = this._bullet_scene.Instantiate<Bullet>();

          backward.GlobalPosition
            = this.GlobalPosition + (direction_b * ShotOffset);

          backward.Setup(direction_b);
          this.GetTree().Root.AddChild(backward);
        }
        yield return null;
      }

      total -= bullets_this_circle;
    }
  }
}
