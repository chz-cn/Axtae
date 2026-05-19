shader_type canvas_item;

uniform bool blink = false;
uniform float blink_speed : hint_range(1.0, 30.0) = 12;
uniform float hidden_ratio : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec4 color = texture(TEXTURE, UV) * COLOR;
  if (blink && color.a > .0) {
    float phase = fract(TIME * blink_speed);
    if (phase < hidden_ratio) {
      color.a = .0;
    }
  }
}
