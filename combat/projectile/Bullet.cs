
using Godot;

public partial class Bullet : Area2D {
  public const float Speed = 80f; // px/s
  public const float MaxLifeTime = 5f;
  public const uint WorldMask = 1;
  public const int damage = 20;

  private Vector2 _direction = Vector2.Right;
  private float _life_time = 0.0f;

  private const byte MaxAudio = 4;
  private static AudioStream _bullet_audio = ResourceLoader
    .Load<AudioStream>("res://asset/audio/Cowboy_gunshot.wav");
  private static uint _playing = 0;

  public override void _Ready() {
    this.AreaEntered += void (area) => {
      if (area is Bullet) { return; }
      this.QueueFree();
    };
    this.BodyEntered += (body) => {
      if (body is Character.Player.Player player) {
        player.TakeDamage(damage);
        this.QueueFree();
        return;
      }
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
    if (this._life_time > MaxLifeTime) { this.QueueFree(); }
  }

  public void Setup(Vector2 direction) {
    if (direction != Vector2.Zero) {
      this._direction = direction.Normalized();
    }
    this.Rotation = this._direction.Angle();
  }

  public void PlayAudio() {
    uint current = System.Threading.Interlocked.Increment(ref _playing);
    if (current >= MaxAudio) {
      System.Threading.Interlocked.Decrement(ref _playing);
      return;
    }

    AudioStreamPlayer2D audio = new() {
      Stream = _bullet_audio,
      GlobalPosition = this.GlobalPosition
    };
    this.GetTree().Root.AddChild(audio);
    audio.Play();
    audio.Connect("finished", Callable.From(() => {
      System.Threading.Interlocked.Decrement(ref _playing);
      audio.QueueFree();
    }));
  }

  private bool WillHit(Vector2 from, Vector2 to) {
    var state = this.GetWorld2D().DirectSpaceState;
    if (state == null) { return false; }

    var query = PhysicsRayQueryParameters2D.Create(from, to, WorldMask);
    query.CollideWithAreas = false;
    query.CollideWithBodies = true;

    var hit = state.IntersectRay(query);
    return hit.Count > 0;
  }
}
