using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.PuzzleHelper
{
    [CustomEntity("PuzzleHelper/PuzzleFallingBlock", "PuzzleHelper/PuzzleBlock")]
    public class PuzzleBlock : FallingBlock
    {

        public bool ignoreJumpThrus;
        

        public PuzzleBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind, bool climbFall, bool ignoreJumpThrus) : base(position, tile, width, height, finalBoss, behind, climbFall)
        {
        }

        public bool IsRiding(JumpThru jumpThru)
        {
            return !this.ignoreJumpThrus && base.CollideCheckOutside(jumpThru, this.Position + Vector2.UnitY);
        }

        public bool IsRiding(Solid solid)
        {
            return base.CollideCheck(solid, this.Position + Vector2.UnitY);
        }

        public bool OnGround(int downCheck = 1)
        {
            return base.CollideCheck<Solid>(this.Position + Vector2.UnitY * (float)downCheck) ||
                (!this.ignoreJumpThrus && base.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY * (float)downCheck));
        }

        public bool OnGround(Vector2 at, int downCheck = 1)
        {
            Vector2 position = this.Position;
            this.Position = at;
            bool result = this.OnGround(downCheck);
            this.Position = position;
            return result;
        }
    }
}