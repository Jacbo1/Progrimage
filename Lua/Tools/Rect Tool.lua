local started = false
local p1, p2
local layer, tempLayer
local color

function OnMouseDown(pos)
	layer = render.getActiveLayer()
	started = true

	-- Set corner 1
	p1 = pos

	-- Set random color
	color = vec3(math.random(0, 255), math.random(0, 255), math.random(0, 255))

	-- Make temp layer to draw to while dragging
	tempLayer = render.createLayer(1, 1)
end

function OnMouseMoveCanvas(pos)
	if not started then return end

	-- Set corner 2
	p2 = pos

	local min = p1:min(p2)
	local max = p1:max(p2)
	local size = max - min + 1

	tempLayer.pos = min
	tempLayer.size = size
	local image = tempLayer.image
	image:clear()
	image:fillRect(color, 0, 0, size[1], size[2])
	tempLayer:update()
end

function OnMouseUp()
	if not started then return end
	started = false

	local min = p1:min(p2)
	local max = p1:max(p2)
	local size = max - min + 1

	-- Draw to original layer
	local image = layer.image
	image:expandToContain(min, size)
	image:fillRect(color, min - image.pos, size)
	layer:update()

	-- Remove temp layer
	tempLayer:dispose()
end