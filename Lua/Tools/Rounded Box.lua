local mouseDown = false
local corner1, corner2
local color = vec3(255)
local radius = 10
local tempLayer
local activeLayer

function OnMouseDown(pos)
	mouseDown = true 

	-- Set both corners
	corner1 = pos
	corner2 = pos

	activeLayer = render.getActiveLayer()

	-- Make temp layer to draw to while dragging
	tempLayer = render.createLayer(1, 1)
	Draw(tempLayer, corner1, corner2)
end

function OnMouseUp()
	mouseDown = false

	-- Remove temp layer
	tempLayer:dispose()

	-- Draw to original layer
	Draw(activeLayer, corner1, corner2)
end

function OnMouseMoveCanvas(pos)
	if not mouseDown then return end

	-- Set corner 2
	corner2 = pos

	-- Draw to temp layer
	tempLayer.image:clear()
	Draw(tempLayer, corner1, corner2)
end

function Draw(layer, corner1, corner2)
	local pos = corner1:min(corner2)
	local max = corner1:max(corner2)
	local size = max - pos + 1

	-- Expand layer to be able to fit the image
	layer.image:expandToContain(pos, size)
	pos = pos - layer.image.pos
	local image = render.createImage(size[1], size[2])
	image:fillRect(color, vec2(radius, 0), size - vec2(radius * 2, 0))
	image:fillRect(color, vec2(0, radius), vec2(size[1], size[2] - radius * 2))
	image:fillOval(color, vec2(radius), vec2(radius * 2))
	image:fillOval(color, vec2(size[1] - radius, radius), vec2(radius * 2))
	image:fillOval(color, vec2(radius, size[2] - radius), vec2(radius * 2))
	image:fillOval(color, size - radius, vec2(radius * 2))
	layer.image:drawImage(image, pos)
	image:dispose()
	layer:update()
end