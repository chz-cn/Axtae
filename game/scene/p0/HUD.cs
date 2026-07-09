
using Godot;

namespace Game.Scene.P0;

public partial class HUD : CanvasLayer {
  public override void _Ready() {
    this.GetTree().Root.SizeChanged += this.Resize;
    this.Resize();
  }

  public override void _ExitTree() {
    this.GetTree().Root.SizeChanged -= this.Resize;
  }

  void Resize() => Callable.From(() =>
    this.Scale = this.GetViewport().GetCamera2D()?.Zoom ?? Vector2.One
  ).CallDeferred();
}
