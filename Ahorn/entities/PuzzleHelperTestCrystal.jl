module PuzzleHelperTestCrystal

using ..Ahorn, Maple


@mapdef Entity "PuzzleHelper/TestCrystal" TestCrystal(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Test Crystal" => Ahorn.EntityPlacement(
        TestCrystal
    )
)

sprite = "characters/theoCrystal/idle00.png"

function Ahorn.selection(entity::TestCrystal)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y - 10)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TestCrystal, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, -10)

end