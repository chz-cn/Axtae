
using System;
using Godot;
using RLMatrix.Toolkit;

namespace Game.Scene.P0;

// [RLMatrixEnvironment]
public partial class Home : Node2D {
  private readonly PackedScene _main_scene = ResourceLoader
    .Load<PackedScene>(Url.Tscn.Main);
  private Main? _main;

  private readonly Label _health_label;
  private readonly Label _form_label;
  private readonly Label _time_label;
  private readonly Label _die_count_label;
  private uint _die_count = 0;
  private double _time = 0;

  public Home() {
    this._health_label = new() {
      Size = new(25, 20),
      Position = new(50, 25),
      Text = "x5"
    };

    this._form_label = new() {
      Size = new(100, 25),
      Position = new(10, 50),
      Text = "Running..."
    };

    this._time_label = new() {
      Size = new(100, 25),
      Position = new(10, 80),
      Text = "0"
    };

    this._die_count_label = new() {
      Size = new(100, 25),
      Position = new(10, 110),
      Text = "0"
    };
  }

  public override void _EnterTree() {
    var main = this._main_scene.Instantiate();

    this.AddChild(main);
    this._main = main as Main;

    var HUD = ResourceLoader.Load<PackedScene>(Url.Tscn.HUD).Instantiate();

    HUD.AddChild(this._health_label);
    HUD.AddChild(this._form_label);
    HUD.AddChild(this._time_label);
    HUD.AddChild(this._die_count_label);
    this.AddChild(HUD);
  }

  public override void _Process(double delta) {
    this._time += delta;
    this._time_label.Text = this._time.ToString("F2");

    if (this._main is null) return;
    if (this._main.Player.Health == 0) {
      this._main.QueueFree();

      var main = this._main_scene.Instantiate() as Main;
      if (this._die_count % 2 == 0) main?.GlobalPosition = new(300, 300);

      this.AddChild(main);
      this._main = main;

      this._die_count_label.Text = (++this._die_count).ToString();

#pragma warning disable S1215 // "GC.Collect" should not be called
      GC.Collect();
#pragma warning restore S1215 // "GC.Collect" should not be called
      return;
    }
    this._health_label.Text = this._main.Player.Health.ToString() ?? "?";
  }

  [RLMatrixObservation]
  public float[] GetState() {
    // player(2) + hp(1) + wall (8) + enemy 12(36) + pickup 5(15)
    var arr = new float[62];
    var span = arr.AsSpan();
    var p = this._main?.Player.GlobalPosition ?? Vector2.Zero;
    span[0] = p.X;
    span[1] = p.Y;

    return arr;
  }

#pragma warning disable S1186 // Methods should not be empty
#pragma warning disable S3400 // Methods should not return constants
#pragma warning disable CA1822 // 将成员标记为 static
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable IDE0060 // 删除未使用的参数
  [RLMatrixActionContinuous]
  public void Step(float[] actions) {
  }

  [RLMatrixReward]
  public float GetReward() {
    return 0f;
  }

  [RLMatrixDone]
  public bool IsDone() {
    return false;
  }

  [RLMatrixReset]
  public void ResetWorld() {
  }
#pragma warning restore IDE0060 // 删除未使用的参数
#pragma warning restore RCS1163 // Unused parameter
#pragma warning restore CA1822 // 将成员标记为 static
#pragma warning restore S3400 // Methods should not return constants
#pragma warning restore S1186 // Methods should not be empty
}
