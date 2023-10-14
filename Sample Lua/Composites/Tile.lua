function Run(image)
	local copy = render.createImage(image.width, image.height)
	copy.pos = image.pos
	copy:drawReplace(image)
	
	-- Tile image to fill canvas
	local canvasSize = render.getCanvasSize()
	for x = 0, canvasSize[1] / copy.width do
		for y = 0, canvasSize[2] / copy.height do
			image:drawReplace(copy, x * copy.width, y * copy.height, true)
		end
	end

	-- Recenter image
	image.pos = image.pos - image.size / 2 + copy.size / 2
	copy:dispose()
end