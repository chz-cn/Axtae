/**
 * characters/player/Player.cs
 * edit 2026.05.13
 */

using Godot;

public partial class Player : CharacterBody2D {
  public const float Speed = 30.0f;
  public const float ShootDelay = .4f;
  public const float offset = 16.0f;

  private AnimatedSprite2D? _sprite;
  private StringName _facing_suffix = "right";
  private PackedScene? _bulletScene;
  private float _shootCooldown = 0.0f;

  public override void _Ready() {
    this._sprite = this.GetNode<AnimatedSprite2D>("Body");
    if (this._sprite == null) {
      GD.PrintErr("Player sprite not found");
    }

    this._bulletScene
      = ResourceLoader.Load<PackedScene>("res://anim/Bullet.tscn");
    if (this._bulletScene == null) GD.PrintErr("Failed to load bullet scene");
  }

  public override void _PhysicsProcess(double delta) {
    Vector2 input = Input.GetVector(
      "move_left",
      "move_right",
      "move_up",
      "move_down");

    if (this._sprite == null) { return; }

    this.Velocity = input.Normalized() * Speed;
    this.MoveAndSlide();

    if (input != Vector2.Zero) {
      this._facing_suffix = Vector2FacingSuffix(input);
    }
    this.UpdateAnimation();

    if (this._shootCooldown > 0) this._shootCooldown -= (float)delta;

    if (Input.IsActionPressed("shoot") && this._shootCooldown <= 0) {
      this.Shoot();
      this._shootCooldown = ShootDelay;
    }
  }

  private void UpdateAnimation() {
    StringName name = this._facing_suffix;

    if (this._sprite == null) { return; }
    if (!this._sprite.SpriteFrames.HasAnimation(name)) {
      GD.PushWarning(name + " not find");
      return;
    }

    if (this._sprite.Animation != name) { this._sprite.Play(name); }
  }

  private static StringName Vector2FacingSuffix(Vector2 input) {
    return (Mathf.Abs(input.X) >= Mathf.Abs(input.Y))
      ? (input.X > .0 ? "right" : "left")
      : (input.Y > .0 ? "down" : "up");
  }

  private Vector2 GetShootDirection() {
    return this._facing_suffix.ToString() switch {
      "right" => Vector2.Right,
      "left" => Vector2.Left,
      "up" => Vector2.Up,
      "down" => Vector2.Down,
      _ => Vector2.Right
    };
  }

  private void Shoot() {
    if (this._bulletScene == null) return;

    Bullet bullet = this._bulletScene.Instantiate<Bullet>();

    bullet.GlobalPosition = this.GlobalPosition
      + this.GetShootDirection() * offset;
    bullet.Setup(this.GetShootDirection());

    this.GetTree().Root.AddChild(bullet);
  }
}
