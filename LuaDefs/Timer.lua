local simpleTimers = {}
local intervalTimers = {}

timer = {
	shouldYield = function()
		return os.clock() - timer.frameStartTimeClock > 0.0126666667
	end,

	simple = function(delay, callback)
		if delay <= 0 then
			callback(0)
			return
		end

		table.insert(simpleTimers, {os.clock() + delay, callback})
	end,

	create = function(name, reps, interval, callback)
		intervalTimers[name] = {reps, interval, callback, os.clock(), 0}
	end,

	remove = function(name)
		intervalTimers[name] = nil
	end,

	setRepetitions = function(name, reps)
		local timer = intervalTimers[name]
		if timer then timer[1] = reps end
	end,

	setInterval = function(name, interval)
		local timer = intervalTimers[name]
		if timer then timer[2] = interval end
	end,

	setCallback = function(name, callback)
		local timer = intervalTimers[name]
		if timer then timer[3] = callback end
	end
}

return function()
	local time = os.clock()

	-- Call simple timers
	local i = 1
	while i <= #simpleTimers do
		local timer = simpleTimers[i]
		if time >= timer[1] then
			-- Execute and delete timer
			timer[2](time - timer[1])
			table.remove(simpleTimers, i)
		else
			i = i + 1
		end
	end

	-- Call interval timers
	local remove = {}
	for name, timer in pairs(intervalTimers) do
		local interval = timer[2]
		if time >= timer[4] + interval then
			-- Execute timer
			-- Offset new start to account for excess elapsed time
			local error = time - timer[4] - interval
			timer[4] = time - error

			-- Run callback
			timer[3](error)

			if timer[1] ~= 0 then
				-- Increment repetition counter
				timer[5] = timer[5] + 1

				if timer[5] >= timer[1] then
					-- Reached max iterations
					table.insert(remove, name)
				end
			end
		end
	end

	-- Remove finished interval timers
	for _, name in ipairs(remove) do
		intervalTimers[name] = nil
	end
end