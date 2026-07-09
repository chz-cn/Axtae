
using Godot;
using L = Game.Config.Layer;

namespace Game.Combat.Projectile;

public partial class Bullet : Area2D {
  public const float Speed = 320f; // px/s
  public const float MaxLifeTime = 2f;
  public const uint Mask = L.CharacterBody | L.World;
  public const byte MaxAudio = 4;
  public uint Damage { get; init; } = 20;

  private Vector2 _direction = Vector2.Zero;
  private float _life_time = 0.0f;

  public static readonly AudioStream BulletAudio = ResourceLoader
    .Load<AudioStream>(Url.Wav.CowboyGunshot);
  private static uint _playing = 0;

  public override void _Ready() {
    if (this._direction == Vector2.Zero) this.QueueFree();

    this.BodyEntered += (body) => {
      if (body is ITakeDamage player)
        player.TakeDamage(this.Damage);

      this.QueueFree();
    };
  }

  public override void _PhysicsProcess(double delta) {
    this.GlobalPosition += this._direction * Speed * (float)delta;
    this._life_time += (float)delta;
    if (this._life_time > MaxLifeTime) this.QueueFree();
  }

  public void Setup(Vector2 direction) {
    this._direction = direction.Normalized();
    this.Rotation = direction.Angle();
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
    audio.Finished += () => {
      System.Threading.Interlocked.Decrement(ref _playing);
      audio.QueueFree();
    };
  }
}
