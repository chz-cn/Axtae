
using Godot;

namespace Config;

[GlobalClass]
public partial class Pickup : Resource {
  public enum Type : byte {
    Speed,
    Rapid,
    Spiral
  }

  [ExportGroup("基础属性")]

  /// <summary>道具类型</summary>
  [Export] public Type CurrentType = Type.Speed;

  /// <summary>道具名称</summary>
  [Export] public StringName DisplayName = "default";

  /// <summary>掉落权重，0 为不参与掉落</summary>
  [Export(PropertyHint.Range, "0.0, 1000.0, 0.1")]
  public float DropWeidth = 1.0f;

  [ExportGroup("显示资源")]

  /// <summary>静态图标资源</summary>
  [Export] public Texture2D? Icon = null;

  [ExportGroup("Buff")]

  /// <summary>持续时间，单位 秒</summary>
  [Export(PropertyHint.Range, "0.0, 120.0, 0.1")]
  public float Duration = 5.0f;

  /// <summary>玩家移速</summary>
  [Export(PropertyHint.Range, "0.1, 5.0, 0.1")]
  public float MoveSpeedMultplier = 1.0f;

  /// <summary>射击速度</summary>
  [Export(PropertyHint.Range, "0.1, 5.0, 0.1")]
  public float FireRateMultplier = 1f;

  [ExportGroup("形态与射速")]

  /// <summary>玩家形态</summary>
  [Export] public Character.Form FormMode = Character.Form.Normal;

  [Export]
  public Character.ShotPattern ShotPattern = Character.ShotPattern.Normal;
}
