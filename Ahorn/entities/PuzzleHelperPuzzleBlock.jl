module PuzzleHelperPuzzleBlock

using ..Ahorn, Maple


@mapdef Entity "Puzzlehelper/PuzzleFallingBlock" PuzzleBlock(x::Integer, y::Integer, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, tiletype::String="3", climbFall::Bool=true, behind::Bool=false, ignoreJumpThru::Bool=false)

const placements = Ahorn.PlacementDict(
    "Puzzle Falling Block" => Ahorn.EntityPlacement(
        PuzzleBlock,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.tileEntityFinalizer
    ),
)

Ahorn.editingOptions(entity::PuzzleBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::PuzzleBlock) = 8, 8
Ahorn.resizable(entity::PuzzleBlock) = true, true

Ahorn.selection(entity::PuzzleBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::PuzzleBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

end