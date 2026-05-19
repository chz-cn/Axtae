
using Godot;

namespace Config;

[GlobalClass]
public partial class Character : Resource {
  public enum Form : byte { Normal, Armed }

  public enum FacingDirection : byte { Right, Left, Up, Down }

  public enum ShotPattern : byte { Normal, Special }

  public static StringName Form2Prefix(Form form)
    => form switch {
      Form.Normal => "n_",
      Form.Armed => "armed_",
      _ => "n_"
    };

  public static StringName Facing2Suffix(FacingDirection facing)
    => facing switch {
      FacingDirection.Right => "right",
      FacingDirection.Left => "left",
      FacingDirection.Up => "up",
      FacingDirection.Down => "down",
      _ => "right"
    };
}
