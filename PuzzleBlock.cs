using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.PuzzleHelper
{
    [CustomEntity("PuzzleHelper/PuzzleFallingBlock")] // same as fallingBlock but should land on moving platforms and move with them
    public class PuzzleBlock : FallingBlock
    {   
        public PuzzleBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind, bool climbFall, bool ignoreJumpThrus)
            : base(position, tile, width, height, finalBoss, behind, climbFall)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", "New instance of PuzzleBlock");

        }

        public PuzzleBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height, finalBoss: false, data.Bool("behind"), data.Bool("climbFall", defaultValue: true), data.Bool("ignoreJumpThrus", defaultValue: false))
        {
        }

        private void onJumpThruCollide(JumpThru jumpThru)
        {
            Logger.Log(LogLevel.Debug, "PuzzleHelepr", jumpThru.ToString());
        }

        public override void Update()
        {
            base.Update();
            base.CollideDo<JumpThru>(new Action<JumpThru>(this.onJumpThruCollide), base.BottomCenter + new Vector2(0, 1));
        }
    }
}