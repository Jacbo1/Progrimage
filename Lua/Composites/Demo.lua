function Run(image)
	local mask = render.createImage(image.width, image.height)

	-- Oval mask
	mask:fillOval(vec3(), mask.size/2, mask.size)
	image:drawMask(mask, 0, 0)

	-- Blue gradient
	local gradient = render.createImage(image.width, image.height)
	for y = 0, gradient.height - 1 do
		gradient:drawLine(vec4(0, 0, 255, y / gradient.height * 255), 1, 0, y, gradient.width, y)
	end
	gradient:drawMask(mask, 0, 0)
	image:drawImage(gradient, 0, 0)
	gradient:dispose()
	mask:dispose()

	image:drawRect(vec3(255,0,0), 20, 524, 326, 1000, 500)
	image:fillPolygon(vec4(0,255,0,200), {vec2(524,826), vec2(1024,326), vec2(1524, 826)})
	image:drawQuadraticCurve(vec4(255,140,0,200), 20, vec2(image.width/2, 0), image.size/2, vec2(image.width, image.height/2))

	-- Copy image 4 times
	local image2 = image:clone()
	image2:resize(image2.size/2, "boxp")
	image:drawReplace(image2, 0, 0)
	image:drawReplace(image2, image.width/2, 0)
	image:drawReplace(image2, 0, image.height/2)
	image:drawReplace(image2, image.size/2)
	image2:dispose()

	-- This part is slow
	--[[local w, h = image.width, image.height
	for x = 0, w - 1 do
		for y = 0, h - 1 do
			local pixel = image:getPixel(x, y)
			pixel[4] = pixel[4] * y / (h - 1)
			image:setPixel(x, y, pixel)
		end
	end]]
end