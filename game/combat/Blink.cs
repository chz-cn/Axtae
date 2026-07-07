
using Godot;

namespace Game.Combat;

interface IBlinkable {
  static readonly Shader Shader = ResourceLoader.Load<Shader>
      ("res://game/pickup/sence/blink.gdshader");

  float BlinkSpeed { get; }
  float HiddenRatio { get; }
  bool Blink { get; }
}
