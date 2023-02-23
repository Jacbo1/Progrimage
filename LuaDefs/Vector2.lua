vector2 = {
	perpendicular = function(vec)
		return vec2(vec[2], -vec[1])
	end,

	dot = function(a, b)
		return a[1] * b[1] + a[2] * b[2]
	end,

	lengthSqr = function(a)
		return a[1] * a[1] + a[2] * a[2]
	end,

	length = function(a)
		return math.sqrt(a[1] * a[1] + a[2] * a[2])
	end,

	distanceSqr = function(a, b)
		local x = a[1] - b[1]
		local y = a[2] - b[2]
		return x * x + y * y
	end,

	distance = function(a, b)
		local x = a[1] - b[1]
		local y = a[2] - b[2]
		return math.sqrt(x * x + y * y)
	end,

	floor = function(vec)
		return vec2(
			math.floor(vec[1]),
			math.floor(vec[2])
		)
	end,

	ceil = function(vec)
		return vec2(
			math.ceil(vec[1]),
			math.ceil(vec[2])
		)
	end,

	round = function(vec)
		return vec2(
			math.floor(vec[1] + 0.5),
			math.floor(vec[2] + 0.5)
		)
	end,

	min = function(a, b)
		return vec2(
			math.min(a[1], b[1]),
			math.min(a[2], b[2])
		)
	end,

	max = function(a, b)
		return vec2(
			math.max(a[1], b[1]),
			math.max(a[2], b[2])
		)
	end,

	toVec3 = function(vec)
		return vec3(vec[1], vec[2])
	end,

	toVec4 = function(vec)
		return vec4(vec[1], vec[2])
	end
}
vector2.__index = vector2

function vec2(x, y)
	return setmetatable({x or 0, y or x or 0}, vector2)
end

function vector2.__add(a, b)
	if type(a) == "number" then return vec2(a + b[1], a + b[2]) end
	if type(b) == "number" then return vec2(a[1] + b, a[2] + b) end
	return vec2(a[1] + b[1], a[2] + b[2])
end

function vector2.__sub(a, b)
	if type(a) == "number" then return vec2(a - b[1], a - b[2]) end
	if type(b) == "number" then return vec2(a[1] - b, a[2] - b) end
	return vec2(a[1] - b[1], a[2] - b[2])
end

function vector2.__mul(a, b)
	if type(a) == "number" then return vec2(a * b[1], a * b[2]) end
	if type(b) == "number" then return vec2(a[1] * b, a[2] * b) end
	return vec2(a[1] * b[1], a[2] * b[2])
end

function vector2.__div(a, b)
	if type(a) == "number" then return vec2(a / b[1], a / b[2]) end
	if type(b) == "number" then return vec2(a[1] / b, a[2] / b) end
	return vec2(a[1] / b[1], a[2] / b[2])
end

function vector2.__unm(a)
	return vec2(-a[1], -a[2])
end

function vector2.__eq(a, b)
	return getmetatable(b) == vector2 and a[1] == b[1] and a[2] == b[2]
end