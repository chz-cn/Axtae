
using Godot;
using Cfg = Config.Character.Player.Player;

namespace Pickup.Scene;

public partial class Pickup : Area2D {
  [Export(PropertyHint.Range, "0.0, 10.0, 0.1")]
  protected float BlinkBeforeExpire = 1.5f;

  private readonly Config.IPickup? _config;
  private Sprite2D? _body_sprite;
  private Timer? _timer;
  private bool _is_expiring = false;

  public override void _Ready() {
    if (this._config == null) {
      GD.PrintErr("Minssing Pickup Config");
      this.QueueFree();
      return;
    }
    this._body_sprite = this.GetNode<Sprite2D>("Body");

    this.InitTimer();

    if (this._body_sprite == null)
      GD.PrintErr("Missing pickup sprite");
    else if (this._body_sprite.Material is not ShaderMaterial)
      GD.PrintErr("Missing shader material");
    else if (this._body_sprite.Material is ShaderMaterial shm
      && shm.Shader == null)
      GD.PrintErr("Missing shader program");
  }

  protected void InitTimer() {
    if (this._config == null) return;

    this._timer = new() {
      WaitTime = System.Math.Max(0.1f, this._config.Duration),
      OneShot = true
    };
    this._timer.Timeout += this.QueueFree;
    this.AddChild(this._timer);
    this._timer.Start();

    this.BodyEntered += (area) => {
      if (area is Character.Player.Player player
        && player.ApplyConfig(
          (this._config as Config.IPickup<Cfg>)?.GetPickup(),
          this._config.Duration))
        this.QueueFree();
    };
  }

  public override void _Process(double delta) {
    if (this._is_expiring
      || this._timer == null
      || this._timer.IsStopped()
      || this._timer.TimeLeft > this.BlinkBeforeExpire) return;

    this._is_expiring = true;
    this.SetBlinkEnable(true);
  }

  protected void SetBlinkEnable(bool enable) {
    var sm = this._body_sprite?.Material as ShaderMaterial;
    sm?.SetShaderParameter("blink", enable);
  }
}
