local mouseDown = false
local corner1, corner2
local sigma = 5
local tempLayer
local activeLayer

function OnMouseDown(pos)
	mouseDown = true
	corner1 = pos
	corner2 = pos
	activeLayer = render.getActiveLayer()
	tempLayer = render.createLayer(1, 1)
	Draw(tempLayer, corner1, corner2)
end

function OnMouseUp()
	mouseDown = false
	tempLayer:dispose()
	local pos = corner1:min(corner2)
	local max = corner1:max(corner2)
	local size = max - pos + 1
	util.createUndoRegion(activeLayer, pos, size)
	Draw(activeLayer, corner1, corner2)
end

function OnMouseMoveCanvas(pos)
	if not mouseDown then return end
	corner2 = pos
	tempLayer.image:clear()
	Draw(tempLayer, corner1, corner2)
end

function Draw(layer, corner1, corner2)
	local pos = corner1:min(corner2)
	local max = corner1:max(corner2)
	local size = max - pos + 1
	local temp = activeLayer.image:getSubimage(pos - activeLayer.pos, size)
	temp:blur(sigma)
	layer.image:drawImage(temp, pos - layer.pos, true)
	temp:dispose()
	layer:update()
end