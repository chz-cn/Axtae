/**
 * anim/Bullet.cs
 * edit 2026.05.13
 */

using Godot;

public partial class Bullet : Area2D {
  public const float Speed = 80.0f; // px/s
  public const float MaxLifeTime = 3.0f;
  public const uint WorldMask = 1;
  private Vector2 _direction = Vector2.Right;
  private float _life_time = 0.0f;

  public override void _Ready() {
    this.AreaEntered += void (area) => {
      if (area is Bullet) return;
      this.QueueFree();
    };
  }

  public override void _PhysicsProcess(double delta) {
    Vector2 position = this.GlobalPosition;
    Vector2 next = position + this._direction * Speed * (float)delta;

    if (this.WillHit(position, next)) {
      this.QueueFree();
      return;
    }

    this.GlobalPosition = next;
    this._life_time += (float)delta;
    if (this._life_time > MaxLifeTime) this.QueueFree();
  }

  public void Setup(Vector2 direction) {
    if (direction != Vector2.Zero) this._direction = direction.Normalized();
    this.Rotation = this._direction.Angle();
  }

  private bool WillHit(Vector2 from, Vector2 to) {
    PhysicsDirectSpaceState2D? state = this.GetWorld2D().DirectSpaceState;
    if (state == null) return false;

    var query = PhysicsRayQueryParameters2D.Create(from, to, WorldMask);
    query.CollideWithAreas = false;
    query.CollideWithBodies = true;

    var hit = state.IntersectRay(query);
    return hit.Count > 0;
  }
}
