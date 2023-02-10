local fakeTilesHelper = require("helpers.fake_tiles")

local CustomEntity = {}

CustomEntity.name = "PuzzleHelper/PuzzleFallingBlock"
CustomEntity.placements =  {
    name = "PuzzleFallingBlock",
    data = {
        tiletype = "3",
        behind = false,
        climbFall = true,
        triggerOthers = false,
        triggerDashSwitches = true,
        collectHeart = false,
        springBounce = true,
        springHorizontal = 120.0,
        springVertical = 350.0,
        springVerticalPercent = 30.0,
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
