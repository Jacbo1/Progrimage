input = {}
render = {}

function math.round(x)
	return math.floor(x + 0.5)
end

function math.clamp(x, min, max)
	return math.min(math.max(x, min), max)
end

require "../LuaDefs/Vector2"
require "../LuaDefs/Vector3"
require "../LuaDefs/Vector4"