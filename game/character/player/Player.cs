
using System.Collections;
using Game.Scene;
using Godot;
using static Core.Logger;

using Bullet = Game.Combat.Projectile.Bullet;
using Cfg = Game.Config.Character.Player.Player;
using L = Game.Config.Layer;

namespace Game.Character.Player;

public sealed partial class Player : CharacterBody2D,
  Combat.IBasicTakeDamage,
  ICameraFollowable {
  public const uint Layer = L.CharacterBody;
  public const uint Mask = L.World | L.CharacterBody | L.CharacterSensor
    | L.Pickup | L.Projectile | L.AreaEffect | L.Melee | L.Hazard;

  public const float ShotOffset = 18f;

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

  private IEnumerator? _spiral_shoot = null;

  private Cfg.FacingDirection _facing = Cfg.FacingDirection.Right;

  // timers
  private float _shoot_timer = 0f;
  private float _spiral_accumulator = 0f;

  private readonly uint _mask = (uint)Core.Rng.Shared.NextUInt64();
  public uint MaxHealth { get; init; } = 5;
  public uint Health {
    get => field ^ this._mask;
    private set => field = value ^ this._mask;
  }

#pragma warning disable S3358 // Ternary operators should not be nested
  public static Cfg.FacingDirection Vector2FacingSuffix(Vector2 input)
    => (Mathf.Abs(input.X) >= Mathf.Abs(input.Y))
      ? (input.X > 0f ? Cfg.FacingDirection.Right : Cfg.FacingDirection.Left)
      : (input.Y > 0f ? Cfg.FacingDirection.Down : Cfg.FacingDirection.Up);
#pragma warning restore S3358 // Ternary operators should not be nested

  public Player() {
    this.MotionMode = MotionModeEnum.Floating;
    this.CollisionLayer = Layer;
    this.CollisionMask = Mask;
  }

  #region Godot Lifecycle Overrides

  public override void _EnterTree() {
    if (this.MaxHealth is 0) {
      this.QueueFree();
      return;
    }
    this.Health = this.MaxHealth;

    this._state = this.GetWorld2D().DirectSpaceState;
    this._ray_query = new() {
      CollisionMask = L.World,
      CollideWithAreas = false,
      CollideWithBodies = true,
      Exclude = [this.GetRid()]
    };

    if (this._bullet_scene is null)
      Warning("Failed to loadfile:  bullet scene");
  }

  public override void _ExitTree() => Error("flush");

  public override void _Ready() {
    var body = this.GetNodeOrNull<AnimatedSprite2D>("Body");
    var armed_sprite = this.GetNodeOrNull<AnimatedSprite2D>("ArmedEffect");

    if (body is null) {
      Warning("Missing Player sprite");
      this.QueueFree();
    }
    else body.AnimationFinished += this.QueueFree;

    if (armed_sprite is null)
      Warning("Missing Player armed effect sprite");
    this._body_sprite = body;
    this._armed_effect_sprite = armed_sprite;
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
      else Warning("die animation not found");

      return;
    }

    Vector2 input = Input.GetVector(
      Game.Config.InputMap.MoveLeft,
      Game.Config.InputMap.MoveRight,
      Game.Config.InputMap.MoveUp,
      Game.Config.InputMap.MoveDown);

    this._config.Update(delta);
    this.Velocity = input.Normalized()
      * this._config.MoveSpeed * this._config.SpeedMultiplier;
    _ = this.MoveAndSlide();

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
    var sprite = this._body_sprite;
    if (sprite is null) return;

    var name = Cfg.GetAnim(this._config.FormMode, this._facing);
    if (!sprite.SpriteFrames.HasAnimation(name)) {
      Warning(name + " not found");
      return;
    }

    if (sprite.Animation != name) sprite.Play(name);
  }

  private void UpdateArmedEffect() {
    var sprite = this._armed_effect_sprite;
    if (sprite is null) return;

    if (this._config.FormMode is not Cfg.Form.Armed) {
      sprite.Visible = false;
      if (sprite.IsPlaying())
        sprite.Stop();

      return;
    }

    sprite.Visible = true;
    if (sprite.IsPlaying()) return;

    StringName effect = "default";
    if (sprite.SpriteFrames.HasAnimation(effect))
      sprite.Play(effect);
  }

  private void UpdateShoot(float delta) {
    if (this._shoot_timer > 0) this._shoot_timer -= delta;

    if (Input.IsActionPressed(Game.Config.InputMap.Shoot)
      && this._shoot_timer <= 0) {
      this.Shoot(this.GetGlobalMousePosition());
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
    var ray = this._ray_query;
    var state = this._state;
    if (ray is null
      || state is null
      || direction == Vector2.Zero
      || offset <= 0f) return null;

    ray.From = from;
    ray.To = from + (direction.Normalized() * offset);

    return state.IntersectRay(ray).Count > 0;
  }

  private void Shoot(Vector2 direction) {
    if (this._bullet_scene is null) return;

    var p = this.GlobalPosition;
    var to = p.DirectionTo(direction);
    bool? hit = this.WillHit(to, p, ShotOffset);
    if (hit is null or true) return;

    Bullet bullet = this._bullet_scene.Instantiate<Bullet>();
    bullet.GlobalPosition = p + (to * ShotOffset);

    bullet.Setup(to);
    this.GetTree().Root.AddChild(bullet);
    bullet.PlayAudio();
  }

  private IEnumerator SpiralShoot(ushort total, byte shots_per_circle) {
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

        var p = this.GlobalPosition;

        if (this.WillHit(direction_f, p, ShotOffset) is false) {
          Bullet forward = this._bullet_scene.Instantiate<Bullet>();
          forward.GlobalPosition = p + (direction_f * ShotOffset);

          forward.Setup(direction_f);
          this.GetTree().Root.AddChild(forward);
          forward.PlayAudio();
        }

        if (this.WillHit(direction_b, p, ShotOffset) is false) {
          Bullet backward = this._bullet_scene.Instantiate<Bullet>();

          backward.GlobalPosition = p + (direction_b * ShotOffset);

          backward.Setup(direction_b);
          this.GetTree().Root.AddChild(backward);
        }
        yield return null;
      }

      total -= bullets_this_circle;
    }
  }
}
