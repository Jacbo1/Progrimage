local internalFuncs = require "../LuaDefs/InternalFuncs"

vector4 = {
	dot = function(a, b)
		return a[1] * b[1] + a[2] * b[2] + a[3] * b[3] + a[4] * b[4]
	end,

	lengthSqr = function(a)
		return a[1] * a[1] + a[2] * a[2] + a[3] * a[3] + a[4] * a[4]
	end,

	length = function(a)
		return math.sqrt(a[1] * a[1] + a[2] * a[2] + a[3] * a[3] + a[4] * a[4])
	end,

	distanceSqr = function(a, b)
		local x = a[1] - b[1]
		local y = a[2] - b[2]
		local z = a[3] - b[3]
		local w = a[4] - b[4]
		return x * x + y * y + z * z + w * w
	end,

	distance = function(a, b)
		local x = a[1] - b[1]
		local y = a[2] - b[2]
		local z = a[3] - b[3]
		local w = a[4] - b[4]
		return math.sqrt(x * x + y * y + z * z + w * w)
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

		return vec4(H, S, V, vec[4])
	end,

	hsvToRgb = function(vec)
		local c = vec[3] * vec[2]
		local x = math.clamp(255 * (vec[3] - c + c * (1 - math.abs((vec[1] / 60 % 2) - 1))), 0, 255)
		local n = math.clamp((vec[3] - c) * 255, 0, 255)
		c = math.clamp(vec[3] * 255, 0, 255)
		local i = math.floor(vec[1] / 60)

		if i == 0 then return vec4(c, x, n, vec[4]) end
		if i == 1 then return vec4(x, c, n, vec[4]) end
		if i == 2 then return vec4(n, c, x, vec[4]) end
		if i == 3 then return vec4(n, x, c, vec[4]) end
		if i == 4 then return vec4(x, n, c, vec[4]) end
		return vec4(c, n, x, vec[4])
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

		return vec4(H, S, L, vec[4])
	end,

	hslToRgb = function(vec)
		local c = (1 - math.abs(2 * vec[3] - 1)) * vec[2]
		local x = math.clamp(255 * (vec[3] - c * 0.5 + c * (1 - math.abs((vec[1] / 60 % 2) - 1))), 0, 255)
		local n = math.clamp((vec[3] - c * 0.5) * 255, 0, 255)
		c = math.clamp((vec[3] + c * 0.5) * 255, 0, 255)
		local i = math.floor(vec[1] / 60)

		if i == 0 then return vec4(c, x, n, vec[4]) end
		if i == 1 then return vec4(x, c, n, vec[4]) end
		if i == 2 then return vec4(n, c, x, vec[4]) end
		if i == 3 then return vec4(n, x, c, vec[4]) end
		if i == 4 then return vec4(x, n, c, vec[4]) end
		return vec4(c, n, x, vec[4])
	end,

	blendColor = function(src, over)
		local meta = getmetatable(over)
		if meta == vector3 then
			return vec4(over[1], over[2], over[3], 255)
		end

		if meta == vector4 then
			local iAlpha = (255 - over[4]) / 255
			local alpha = over[4] + src[4] * iAlpha
			if alpha == 0 then return end

			return vec4(
				(over[1] * over[4] + src[1] * src[4] * iAlpha) / alpha,
				(over[2] * over[4] + src[2] * src[4] * iAlpha) / alpha,
				(over[3] * over[4] + src[3] * src[4] * iAlpha) / alpha,
				255)
		end

		error("Cannot blend " .. internalFuncs.type(over) .. " over vector4")
	end,

	floor = function(vec)
		return vec4(
			math.floor(vec[1]),
			math.floor(vec[2]),
			math.floor(vec[3]),
			math.floor(vec[4])
		)
	end,

	ceil = function(vec)
		return vec4(
			math.ceil(vec[1]),
			math.ceil(vec[2]),
			math.ceil(vec[3]),
			math.ceil(vec[4])
		)
	end,

	round = function(vec)
		return vec4(
			math.floor(vec[1] + 0.5),
			math.floor(vec[2] + 0.5),
			math.floor(vec[3] + 0.5),
			math.floor(vec[4] + 0.5)
		)
	end,

	min = function(vec, vec2)
		return vec4(
			math.min(vec[1], vec2[1]),
			math.min(vec[2], vec2[2]),
			math.min(vec[3], vec2[3]),
			math.min(vec[4], vec2[4])
		)
	end,

	max = function(vec, vec2)
		return vec4(
			math.max(vec[1], vec2[1]),
			math.max(vec[2], vec2[2]),
			math.max(vec[3], vec2[3]),
			math.max(vec[4], vec2[4])
		)
	end,

	toVec2 = function(vec)
		return vec2(vec[1], vec[2])
	end,

	toVec3 = function(vec)
		return vec3(vec[1], vec[2], vec[3])
	end
}
vector4.__index = vector4

function vec4(x, y, z, w)
	x = x or 0
	if y then
		z = z or 0
		w = w or 0
	else
		y = x
		z = z or x
		w = w or x
	end
	return setmetatable({x, y, z, w}, vector4)
end

function vector4.__add(a, b)
	if type(a) == "number" then return vec4(a + b[1], a + b[2], a + b[3], a + b[4]) end
	if type(b) == "number" then return vec4(a[1] + b, a[2] + b, a[3] + b, a[4] + b) end
	return vec4(a[1] + b[1], a[2] + b[2], a[3] + b[3], a[4] + b[4])
end

function vector4.__sub(a, b)
	if type(a) == "number" then return vec4(a - b[1], a - b[2], a - b[3], a - b[4]) end
	if type(b) == "number" then return vec4(a[1] - b, a[2] - b, a[3] - b, a[4] - b) end
	return vec4(a[1] - b[1], a[2] - b[2], a[3] - b[3], a[4] - b[4])
end

function vector4.__mul(a, b)
	if type(a) == "number" then return vec4(a * b[1], a * b[2], a * b[3], a * b[4]) end
	if type(b) == "number" then return vec4(a[1] * b, a[2] * b, a[3] * b, a[4] * b) end
	return vec4(a[1] * b[1], a[2] * b[2], a[3] * b[3], a[4] * b[4])
end

function vector4.__div(a, b)
	if type(a) == "number" then return vec4(a / b[1], a / b[2], a / b[3], a / b[4]) end
	if type(b) == "number" then return vec4(a[1] / b, a[2] / b, a[3] / b, a[4] / b) end
	return vec4(a[1] / b[1], a[2] / b[2], a[3] / b[3], a[4] / b[4])
end

function vector4.__mod(a, b)
	if type(a) == "number" then return vec4(a % b[1], a % b[2], a % b[3], a % b[4]) end
	if type(b) == "number" then return vec4(a[1] % b, a[2] % b, a[3] % b, a[4] % b) end
	return vec4(a[1] % b[1], a[2] % b[2], a[3] % b[3], a[4] % b[4])
end

function vector4.__pow(a, b)
	if type(a) == "number" then return vec4(a ^ b[1], a ^ b[2], a ^ b[3], a ^ b[4]) end
	if type(b) == "number" then return vec4(a[1] ^ b, a[2] ^ b, a[3] ^ b, a[4] ^ b) end
	return vec4(a[1] ^ b[1], a[2] ^ b[2], a[3] ^ b[3], a[4] ^ b[4])
end

function vector4.__unm(a)
	return vec4(-a[1], -a[2], -a[3], -a[4])
end

function vector4.__eq(a, b)
	return getmetatable(b) == vector4 and a[1] == b[1] and a[2] == b[2] and a[3] == b[3] and a[4] == b[4]
end

function vector4.__concat(a, b)
	return tostring(a) .. tostring(b)
end

function vector3.__tostring(vec)
	return "<" .. vec[1] .. ", " .. vec[2] .. ", " .. vec[3] .. ">"
end