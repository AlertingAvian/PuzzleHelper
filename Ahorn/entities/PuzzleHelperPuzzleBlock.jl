module PuzzleHelperPuzzleBlock

using ..Ahorn, Maple

const defaultBlockWidth = 16
const defaultBlockHeight = 16

@mapdef Entity "Puzzlehelper/PuzzleBlock" PuzzleBlock(x::Integer, y::Integer, width::Integer=defaultBlockWidth, height::Integer=defaultBlockHeight, tiletype::String="3", climbFall::Bool=true, behind::Bool=false, ignoreJumpThru::Bool=false)

const placements = Ahorn.PlacementDict(
    "Puzzle Falling Block" => Ahorn.EntityPlacement(
        PuzzleBlock,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.tileEntityFinalizer
    ),
)

Ahorn.editingOptions(entity::Maple.FallingBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::Maple.FallingBlock) = 8, 8
Ahorn.resizable(entity::Maple.FallingBlock) = true, true

Ahorn.selection(entity::Maple.FallingBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::Maple.FallingBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

function Base.Dict(e::Entity)
    res = Dict{String, Any}()
    res["__name"] = e.name
    res["id"] = e.id

    for (key, value) in e.data
        if !(key in blacklistedEntityAttrs)
            res[key] = value
        end
    end

    if haskey(e.data, "nodes")
        if length(e.data["nodes"]) > 0
            res["__children"] = Dict{String, Any}[]

            for node in e.data["nodes"]
                push!(res["__children"], Dict{String, Any}(
                    "__name" => "node",
                    "x" => node[1],
                    "y" => node[2]
                ))
            end
        end
    end

    return res
end

end