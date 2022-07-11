using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.PuzzleHelper
{
    [CustomEntity("PuzzleHelper/PuzzleBlock")] // same as fallingBlock but should land on moving platforms and move with them
    public class PuzzleBlock : FallingBlock
    {

        public bool ignoreJumpThrus;
        

        public PuzzleBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind, bool climbFall, bool ignoreJumpThrus)
            : base(position, tile, width, height, finalBoss, behind, climbFall)
        {
            Logger.Log(LogLevel.Debug, "PuzzleHelper", "New instance of PuzzleBlock");
            Logger.Log(LogLevel.Debug, "PuzzleHelper", ignoreJumpThrus.ToString());
        }

        public PuzzleBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height, finalBoss: false, data.Bool("behind"), data.Bool("climbFall", defaultValue: true), data.Bool("ignoreJumpThrus", defaultValue: false))
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