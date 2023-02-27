local internalFuncs = require "../LuaDefs/InternalFuncs"

vector3 = {
	dot = function(a, b)
		return a[1] * b[1] + a[2] * b[2] + a[3] * b[3]
	end,

	cross = function(a, b)
		return vec3(a[2] * b[3] - a[3] * b[2], a[3] * b[1] - a[1] * b[3], a[1] * b[2] - a[2] * b[1])
	end,

	lengthSqr = function(a)
		return a[1] * a[1] + a[2] * a[2] + a[3] * a[3]
	end,

	length = function(a)
		return math.sqrt(a[1] * a[1] + a[2] * a[2] + a[3] * a[3])
	end,

	distanceSqr = function(a, b)
		local x = a[1] - b[1]
		local y = a[2] - b[2]
		local z = a[3] - b[3]
		return x * x + y * y + z * z
	end,

	distance = function(a, b)
		local x = a[1] - b[1]
		local y = a[2] - b[2]
		local z = a[3] - b[3]
		return math.sqrt(x * x + y * y + z * z)
	end,

	rgbToHsv = function(vec)
		local r = vec[1] / 255
		local g = vec[2] / 255
		local b = vec[3] / 255

		local V = math.max(r, g, b)
		local delta = V - math.min(r, g, b)

		local H
		if delta == 0 then H = 0
		elseif r == V then H = 60 * ((g - b) / delta % 6)
		elseif g == V then H = 60 * (2 + b - r) / delta
		else H = 60 * (4 + (r - g) / delta) end

		local S = V == 0 and 0 or delta / V

		return vec3(H, S, V)
	end,

	hsvToRgb = function(vec)
		local c = vec[3] * vec[2]
		local x = math.clamp(255 * (vec[3] - c + c * (1 - math.abs((vec[1] / 60 % 2) - 1))), 0, 255)
		local n = math.clamp((vec[3] - c) * 255, 0, 255)
		c = math.clamp(vec[3] * 255, 0, 255)
		local i = math.floor(vec[1] / 60)

		if i == 0 then return vec3(c, x, n) end
		if i == 1 then return vec3(x, c, n) end
		if i == 2 then return vec3(n, c, x) end
		if i == 3 then return vec3(n, x, c) end
		if i == 4 then return vec3(x, n, c) end
		return vec3(c, n, x)
	end,

	rgbToHsl = function(vec)
		local r = vec[1] / 255
		local g = vec[2] / 255
		local b = vec[3] / 255

		local max = math.max(r, g, b)
		local min = math.min(r, g, b)
		local delta = max - min

		local H
		if delta == 0 then H = 0
		elseif r == V then H = 60 * ((g - b) / delta % 6)
		elseif g == V then H = 60 * (2 + b - r) / delta
		else H = 60 * (4 + (r - g) / delta) end

		local L = (max + min) / 2
		local S = delta == 0 and 0 or delta / (1 - math.abs(2 * L - 1))

		return vec3(H, S, L)
	end,

	hslToRgb = function(vec)
		local c = (1 - math.abs(2 * vec[3] - 1)) * vec[2]
		local x = math.clamp(255 * (vec[3] - c * 0.5 + c * (1 - math.abs((vec[1] / 60 % 2) - 1))), 0, 255)
		local n = math.clamp((vec[3] - c * 0.5) * 255, 0, 255)
		c = math.clamp((vec[3] + c * 0.5) * 255, 0, 255)
		local i = math.floor(vec[1] / 60)

		if i == 0 then return vec3(c, x, n) end
		if i == 1 then return vec3(x, c, n) end
		if i == 2 then return vec3(n, c, x) end
		if i == 3 then return vec3(n, x, c) end
		if i == 4 then return vec3(x, n, c) end
		return vec3(c, n, x)
	end,

	blendColor = function(src, over)
		local meta = getmetatable(over)
		if meta == vector3 then
			return vec3(over[1], over[2], over[3])
		end

		if meta == vector4 then
			local overAlpha = over[4] / 255
			local iAlpha = 1 - overAlpha
			return vec3(
				over[1] * overAlpha + src[1] * iAlpha,
				over[2] * overAlpha + src[2] * iAlpha,
				over[3] * overAlpha + src[3] * iAlpha)
		end

		error("Cannot blend " .. internalFuncs.type(over) .. " over vector3")
	end,

	floor = function(vec)
		return vec3(
			math.floor(vec[1]),
			math.floor(vec[2]),
			math.floor(vec[3])
		)
	end,

	ceil = function(vec)
		return vec3(
			math.ceil(vec[1]),
			math.ceil(vec[2]),
			math.ceil(vec[3])
		)
	end,

	round = function(vec)
		return vec3(
			math.floor(vec[1] + 0.5),
			math.floor(vec[2] + 0.5),
			math.floor(vec[3] + 0.5)
		)
	end,

	min = function(vec, vec2)
		return vec3(
			math.min(vec[1], vec2[1]),
			math.min(vec[2], vec2[2]),
			math.min(vec[3], vec2[3])
		)
	end,

	max = function(vec, vec2)
		return vec3(
			math.max(vec[1], vec2[1]),
			math.max(vec[2], vec2[2]),
			math.max(vec[3], vec2[3])
		)
	end,

	toVec2 = function(vec)
		return vec2(vec[1], vec[2])
	end,

	toVec4 = function(vec, w)
		return vec4(vec[1], vec[2], vec[3], w)
	end
}
vector3.__index = vector3

function vec3(x, y, z)
	x = x or 0
	if y then z = z or 0
	else
		y = x
		z = z or x
	end
	return setmetatable({x, y, z}, vector3)
end

function vector3.__add(a, b)
	if type(a) == "number" then return vec3(a + b[1], a + b[2], a + b[3]) end
	if type(b) == "number" then return vec3(a[1] + b, a[2] + b, a[3] + b) end
	return vec3(a[1] + b[1], a[2] + b[2], a[3] + b[3])
end

function vector3.__sub(a, b)
	if type(a) == "number" then return vec3(a - b[1], a - b[2], a - b[3]) end
	if type(b) == "number" then return vec3(a[1] - b, a[2] - b, a[3] - b) end
	return vec3(a[1] - b[1], a[2] - b[2], a[3] - b[3])
end

function vector3.__mul(a, b)
	if type(a) == "number" then return vec3(a * b[1], a * b[2], a * b[3]) end
	if type(b) == "number" then return vec3(a[1] * b, a[2] * b, a[3] * b) end
	return vec3(a[1] * b[1], a[2] * b[2], a[3] * b[3])
end

function vector3.__div(a, b)
	if type(a) == "number" then return vec3(a / b[1], a / b[2], a / b[3]) end
	if type(b) == "number" then return vec3(a[1] / b, a[2] / b, a[3] / b) end
	return vec3(a[1] / b[1], a[2] / b[2], a[3] / b[3])
end

function vector3.__unm(a)
	return vec3(-a[1], -a[2], -a[3])
end

function vector3.__eq(a, b)
	return getmetatable(b) == vector3 and a[1] == b[1] and a[2] == b[2] and a[3] == b[3]
end