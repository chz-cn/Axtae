
using System;
using Game.Combat;
using Game.Config.Pickup;
using Godot;
using static Core.Logger;
using L = Game.Config.Layer;

namespace Game.Pickup.Scene;

public partial class Pickup : Area2D, IBlinkable {
  public const uint Layer = L.Pickup;
  public const uint Mask = L.CharacterBody;

  public float BlinkBeforeExpire { get; init; } = 1.5f;

  #region IBlinkable

  public float BlinkSpeed {
    get; init {
      // 6f is default blink speed
      if (value is not 6f and > .1f and < 30) {
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter(Scene.Blink.BlinkSpeed, value);
        field = value;
      }
    }
  }

  public float HiddenRatio {
    get; init {
      // .5f is default blink speed
      if (value is not .5f and > 0f and < 1f) {
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter(Scene.Blink.HiddenRatio, value);
        field = value;
      }
    }
  }

  public bool Blink {
    get; private set {
      if (field != value) {
        (this._body_sprite.Material as ShaderMaterial)?
          .SetShaderParameter(Scene.Blink.blink, value);
        field = value;
      }
    }
  }

  #endregion

  public Config.IPickup Config { get; }

  private readonly Sprite2D _body_sprite;

  public Pickup(Config.IPickup config) {
    ArgumentNullException.ThrowIfNull(config, nameof(config));
    if (config is Empty)
      throw new ArgumentException("Empty Pickup config", nameof(config));

    ArgumentOutOfRangeException.ThrowIfLessThan(
      config.Duration, .1f, nameof(config.Duration));

    this.CollisionLayer = Layer;
    this.CollisionMask = Mask;

    this.Config = config;

    this._body_sprite = new() {
      Texture = ResourceLoader.Load<Texture2D>(config.TexturePath),
      Material = new ShaderMaterial {
        Shader = IBlinkable.Shader
      }
    };
  }

  public override void _EnterTree() {
    this.AddChild(this._body_sprite);

    this.AddChild(new CollisionShape2D {
      Shape = this.Config.Shape
    });
  }

  public override void _Ready() {
    this.BodyEntered += async (node) => {
      if (node is Character.Player.Player player) {
        (this.Config as Config.IPickup<Character.Player.Player>)?
          .ApplyTo(player);
        this.QueueFree();
      }
    };

    this.InitTimer();

    var sprite = this._body_sprite;

    if (sprite.Texture is null)
      Log(Level.Warning, "Missing pickup texture");

    if (sprite.Material is not ShaderMaterial material)
      Log(Level.Warning, "Missing shader material");
    else if (material.Shader is null)
      Log(Level.Warning, "Missing shader program");
  }

#pragma warning disable S3168 // "async" methods should not return "void"
  private async void InitTimer() {
    try {
      float blink = Math.Clamp(this.BlinkBeforeExpire, 0, this.Config.Duration);
      float before_blink = this.Config.Duration - blink;

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
    }
    catch (Exception ex) {
      Log(Level.Error, ex.Message);
    }
    finally {
      this.QueueFree();
    }
  }
#pragma warning restore S3168 // "async" methods should not return "void"
}
