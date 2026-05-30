shader_type canvas_item;

uniform bool blink = false;
// 闪烁速度：默认值为 6 即每秒闪 6 次。
uniform float blink_speed : hint_range(1.0, 30.0) = 6;
// 隐藏比例：控制单次闪烁中，道具处于“不可见”状态的时间占比。
uniform float hidden_ratio : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec4 color = texture(TEXTURE, UV) * COLOR;
  if (blink && color.a > .0) {
    float phase = fract(TIME * blink_speed);
    if (phase < hidden_ratio) {
      color.a = .0;
    }
  }
  COLOR = color;
}
