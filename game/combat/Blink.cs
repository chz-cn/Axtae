
using Godot;

namespace Game.Combat;

interface IBlinkable {
  static readonly Shader Shader = ResourceLoader
    .Load<Shader>(Url.GDSharder.Blink);

  float BlinkSpeed { get; }
  float HiddenRatio { get; }
  bool IsBlink { get; }
}
