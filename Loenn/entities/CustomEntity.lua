local fakeTilesHelper = require("helpers.fake_tiles")

local CustomEntity = {}

CustomEntity.name = "PuzzleHelper/PuzzleFallingBlockNEW"
CustomEntity.placements =  {
    name = "PuzzleHelper/PuzzleFallingBlockNEW",
    data = {
        tiletype = "3",
        behind = false,
        climbFall = true,
        triggerOthers = false,
        width = 8,
        height = 8
    }
}

CustomEntity.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)
CustomEntity.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype")

function CustomEntity.depth(room, entity)
    return entity.behind and 5000 or 0
end

return CustomEntity
