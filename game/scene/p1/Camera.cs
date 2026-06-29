
using System;
using System.Threading;

using Godot;

namespace Game.Scene.P1;

public interface ICameraFollowable {
  event Action OnExit;
  Vector2 GlobalPosition { get; }
}

public sealed partial class Camera : Camera2D {
  public const int BaseResolutionX = 640;
  public const int BaseResolutionY = 360;

  private ICameraFollowable? _targget;
  public required ICameraFollowable? TargetToFollow {
    get => this._targget;
    set {
      if (value == this._targget) return;

      this._targget?.OnExit -= this.FollowTarget;
      value?.OnExit += this.FollowTarget;

      this._targget = value;
    }
  }

  public Camera() {
    this.Enabled = true;
    this.LimitEnabled = false;
    this.PositionSmoothingEnabled = true;
    this.PositionSmoothingSpeed = 3f;

    this.DragHorizontalEnabled = true;
    this.DragVerticalEnabled = true;

    this.DragBottomMargin = .1f;
    this.DragTopMargin = .1f;
    this.DragLeftMargin = .1f;
    this.DragRightMargin = .1f;
  }

  public override void _Ready() {
    this.GetTree().Root.SizeChanged += this.Resize;
    this.Resize();
  }

  public override void _Process(double delta) {
    var target = this.TargetToFollow;
    if (target is null) return;

    this.GlobalPosition = target.GlobalPosition;
  }

  public void Resize() {
    var (windowX, windowY) = DisplayServer.WindowGetSize();

    int ratioX = windowX / BaseResolutionX;
    int ratioY = windowY / BaseResolutionY;

    int current = Math.Min(ratioX, ratioY);
    current = Math.Max(current, 1);

    this.Zoom = new(current, current);
  }

  private void FollowTarget() {
    if (this._targget is null) return;
    this._targget = null;
  }
}
