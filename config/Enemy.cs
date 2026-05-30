
using Godot;

namespace Config;

public partial class Enemy : Resource {
  /// <summary>静态图标资源</summary>
  [Export] public SpriteFrames? Frame = null;

  /// <summary>移动动画</summary>
  [Export] public StringName MoveAnim = "move";

  // /// <summary>死亡动画</summary>
  [Export] public StringName DieAnim = "die";

  /// <summary>生命值</summary>
  public int MaxHealth = 3;

  /// <summary>移速：px/s</summary>
  public float Speed = 30;

  /// <summary>掉落率：%</summary>
  public float DropRate = 0.0f;

  /// <summary>掉落物</summary>
  [Export] public Pickup[] Drop = [];

  /// <summary>掉落权重: uint</summary>
  [Export] public int[] DropWeights = [];
}
