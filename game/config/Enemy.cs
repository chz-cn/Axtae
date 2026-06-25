
using Godot;

namespace Game.Config;

public partial class Enemy {
  /// <summary>静态图标资源</summary>
  public SpriteFrames? Frame = null;

  /// <summary>移动动画</summary>
  public StringName MoveAnim = "move";

  // /// <summary>死亡动画</summary>
  public StringName DieAnim = "die";

  /// <summary>生命值</summary>
  public int MaxHealth = 3;

  /// <summary>移速：px/s</summary>
  public float Speed = 30;

  /// <summary>掉落率：%</summary>
  public float DropRate = 0.0f;

  /// <summary>掉落物 掉落权重: uint</summary>/// <summary>掉落物 掉落权重: uint</summary>
  public System.Tuple<IPickup, uint>[] Drop = [];

}
