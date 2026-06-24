
using System;
using System.Threading;
using System.Threading.Tasks;
using Character.Player;
using Godot;

namespace Scene.P1;

public interface ICameraFollowable {
  Task OnExit { get; }
  Vector2 GlobalPosition { get; }
}

public sealed partial class Camera : Camera2D {
  public const int BaseResolutionX = 640;
  public const int BaseResolutionY = 360;

  public required ICameraFollowable? TargetToFollow {
    get;
    set {
      this.CleanCTS();
      field = value;
      if (value is not null) {
        this._cts = new();
        this.FollowTarget(this._cts.Token);
      }
    }
  }

  private CancellationTokenSource? _cts;

  public Camera() {
    this.Enabled = true;
    this.LimitEnabled = false;
    this.PositionSmoothingEnabled = true;
    this.PositionSmoothingSpeed = 1f;

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

  private void CleanCTS() {
    if (this._cts is not null) {
      this._cts.Cancel();
      this._cts.Dispose();
      this._cts = null;
    }
  }

  private async void FollowTarget(CancellationToken token) {
    var target = this.TargetToFollow;
    if (target is null) return;

    try {
      await target.OnExit.WaitAsync(token);
      this.TargetToFollow = null;
      this.CleanCTS();
    }
    catch (OperationCanceledException) { }
    catch (Exception ex) {
      GD.PrintErr($"Camera: Error while waiting for target exit: {ex.Message}");
    }
  }
}
