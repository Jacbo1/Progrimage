return {
    type = function(n)
        local type_ = type(n)
        if type_ ~= "table" then return type_ end
        local meta = getmetatable(n)
        if meta == vector2 then return "vector2" end
        if meta == vector3 then return "vector3" end
        if meta == vector4 then return "vector4" end
        return type_
    end
}