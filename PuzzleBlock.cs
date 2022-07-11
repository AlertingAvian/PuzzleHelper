using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.PuzzleHelper
{
    [CustomEntity("PuzzleHelper/PuzzleFallingBlock", "PuzzleHelper/PuzzleBlock")]
    public class PuzzleBlock : FallingBlock
    {
        public PuzzleBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind, bool climbFall) : base(position, tile, width, height, finalBoss, behind, climbFall)
        {

        }

        public PuzzleBlock(EntityData data, Vector2 offset) : this(data.Position + offset, 'a', 1, 1, false, false, false) // not set up properly
        {

        }
    }
}