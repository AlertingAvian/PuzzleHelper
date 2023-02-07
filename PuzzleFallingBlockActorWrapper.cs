using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Reflection;


namespace Celeste.Mod.PuzzleHelper
{
    public class PuzzleFallingBlockActorWrapper : Actor
    {
        public PuzzleFallingBlock block { get; private set; }

        public PuzzleFallingBlockActorWrapper(PuzzleFallingBlock block) : base(block.Position)
        {
            this.block = block;
            TreatNaive = false;
            Hitbox hitbox = new Hitbox(block.Width, block.Height);
            base.Collider = hitbox;
            AllowPushing = true;
        }

        public override void Update()
        {
            
        }

        public override bool IsRiding(JumpThru jumpThru)
        {
            return block.IsRiding(jumpThru);
        }

        public override bool IsRiding(Solid solid)
        {
            if (!(solid is PuzzleFallingBlock))
            {
                return block.IsRiding(solid);
            }
            return false;
        }
    }
}
