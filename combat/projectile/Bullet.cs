
using Godot;
using L = Config.Layer;

namespace Combat.Projectile;

public partial class Bullet : Area2D {
  public const float Speed = 80f; // px/s
  public const float MaxLifeTime = 5f;
  public const uint Mask = L.EnemyBody | L.World | L.Player;
  public const byte MaxAudio = 4;
  public const int damage = 20;

  public Godot.Collections.Array<Rid>? Exclude { get; set; }

  private Vector2 _direction = Vector2.Right;
  private float _life_time = 0.0f;
  private PhysicsRayQueryParameters2D _ray_query = new() {
    CollisionMask = Mask,
    CollideWithAreas = false,
    CollideWithBodies = true,
    HitFromInside = true
  };

  public static readonly AudioStream BulletAudio = ResourceLoader
    .Load<AudioStream>("res://asset/audio/Cowboy_gunshot.wav");
  private static uint _playing = 0;

  public override void _Ready()
    => this.BodyEntered += (body) => {
      if (body is Character.Player.Player player)
        player.TakeDamage(damage);

      this.QueueFree();
    };

  public override void _PhysicsProcess(double delta) {
    Vector2 next = this.GlobalPosition + this._direction * Speed * (float)delta;

    this.GlobalPosition = next;
    this._life_time += (float)delta;
    if (this._life_time > MaxLifeTime) this.QueueFree();
  }

  public void Setup(
    Vector2 direction,
    Vector2 from,
    float offset,
    PhysicsDirectSpaceState2D? state) {
    Vector2 to = from + this._direction * offset;
    this._ray_query.From = from;
    this._ray_query.To = to;
    if (this.Exclude != null) this._ray_query.Exclude = this.Exclude;

    var hit = state?.IntersectRay(this._ray_query);
    if (hit == null || hit.Count <= 0) this.QueueFree();

    if (direction != Vector2.Zero) this._direction = direction.Normalized();
    this.Rotation = this._direction.Angle();
    this.GlobalPosition = from + this._direction * offset;
  }

  public void PlayAudio() {
    uint current = System.Threading.Interlocked.Increment(ref _playing);
    if (current >= MaxAudio) {
      System.Threading.Interlocked.Decrement(ref _playing);
      return;
    }

    AudioStreamPlayer2D audio = new() {
      Stream = BulletAudio,
      GlobalPosition = this.GlobalPosition
    };
    this.GetTree().Root.AddChild(audio);
    audio.Play();
    audio.Connect("finished", Callable.From(() => {
      System.Threading.Interlocked.Decrement(ref _playing);
      audio.QueueFree();
    }));
  }
}
