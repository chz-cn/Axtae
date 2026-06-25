
using Godot;
using Cfg = Game.Config.Character.Player.Player;
using L = Game.Config.Layer;

namespace Pickup.Scene;

public partial class Pickup : Area2D {
  public const uint Layer = L.Pickup;
  public const uint Mask = L.CharacterBody;

  public float BlinkBeforeExpire { get; init; } = 1.5f;

  public float BlinkSpeed {
    get; init {
      field = value;
      // 6f is default blink speed
      if (value != 6f && value > .1f)
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter("blink_speed", value);
    }
  }

  public float HiddenRatio {
    get; init {
      field = value;
      // .5f is default blink speed
      if (value != .5f && value > 0f && value < 1f)
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter("blink_speed", value);
    }
  }

  private bool _blink = false;
  public bool Blink {
    get => this.Blink;
    private set {
      if (this._blink != value)
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter("blink", value);
    }
  }

  public float Radius { get; init; } = 6f;

  public required Game.Config.IPickup Config { get; init; }

  private readonly Sprite2D _body_sprite;

  public Pickup(string texture_path) {
    this.CollisionLayer = Layer;
    this.CollisionMask = Mask;

    this._body_sprite = new() {
      Texture = ResourceLoader.Load<Texture2D>(texture_path),
      Material = new ShaderMaterial() {
        Shader = ResourceLoader
          .Load<Shader>("res://game/pickup/sence/blink.gdshader")
      }
    };
  }

  public override void _EnterTree() {
    this.AddChild(this._body_sprite);

    this.AddChild(new CollisionShape2D() {
      Shape = new CircleShape2D() {
        Radius = this.Radius
      }
    });
  }

  public override void _Ready() {
    this.BodyEntered += (area) => {
      if (area is Game.Character.Player.Player player) {
        var cfg = (this.Config as Game.Config.IPickup<Cfg>)?.GetPickup();
        if (cfg is not null) {
          if (player.ApplyConfig(cfg.Value, this.Config.Duration))
            this.QueueFree();
        }
        else
          GD.PrintErr("Missing pickup config for player");
      }
    };

    this.InitTimer();

    if (this._body_sprite is null) GD.PrintErr("Missing pickup sprite");
    else {
      if (this._body_sprite.Texture is null)
        GD.PrintErr("Missing pickup texture");

      if (this._body_sprite.Material is not ShaderMaterial)
        GD.PrintErr("Missing shader material");
      else if (this._body_sprite.Material is ShaderMaterial shm
        && shm.Shader is null)
        GD.PrintErr("Missing shader program");
    }
  }

  private async void InitTimer() {
    float total = System.MathF.Max(.1f, this.Config.Duration);
    float blink = System.Math.Clamp(this.BlinkBeforeExpire, 0, total);
    float before_blink = total - blink;

    if (before_blink > 0.1f)
      await this.ToSignal(
        this.GetTree().CreateTimer(before_blink),
        Timer.SignalName.Timeout);

    if (blink > 0.1f) {
      this.Blink = true;
      await this.ToSignal(
        this.GetTree().CreateTimer(blink),
        Timer.SignalName.Timeout);
    }

    this.QueueFree();
  }
}
