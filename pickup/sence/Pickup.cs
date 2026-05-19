
using Godot;

namespace Pickup.Scene;

public partial class Pickup : Area2D {
  [Export(PropertyHint.Range, "0.0, 10.0, 1.0")]
  private float BlinkBeforeExpire = 1.5f;

  [Export] private Config.Pickup? _config;
  private Sprite2D? _body_sprite;
  private Timer? _timer;
  private bool _is_expiring = false;

  public override void _Ready() {
    this._body_sprite = this.GetNode<Sprite2D>("Body");

    this._timer = new() {
      WaitTime = this._config?.Duration ?? 5f,
      OneShot = true
    };
    this._timer.Timeout += this.QueueFree;
    this.AddChild(this._timer);
    this._timer.Start();

    this.BodyEntered += (area) => {
      if (this._config == null) return;
      if (area is Character.Player.Player player) {
        if (player.ApplyPickup(this._config)) this.QueueFree();
      }
    };

    this.ApplyConfig();

    if (this._config == null) GD.PrintErr("Minssing Pickup Config");
    if (this._body_sprite == null) GD.PrintErr("Missing pickup sprite");
    else if (this._body_sprite.Material == null)
      GD.PrintErr("Missing gdsharder");
    else if (this._body_sprite.Material is ShaderMaterial shm)
      if (shm.Shader == null) GD.PrintErr("Missing shader program");
  }

  public override void _Process(double delta) {
    if (this._is_expiring
      || this._timer == null
      || this._timer.IsStopped()
      || this._timer.TimeLeft > this.BlinkBeforeExpire) return;

    this._is_expiring = true;
    this.SetBlinkEnable(true);
  }

  private void ApplyConfig() {
    if (this._body_sprite == null || this._config == null) return;
    this._body_sprite.Texture = this._config.Icon;
  }

  private void SetBlinkEnable(bool enable) {
    var sm = this._body_sprite?.Material as ShaderMaterial;
    sm?.SetShaderParameter("blink", enable);
  }
}
