using Monocle;
using System;

namespace Celeste.Mod.PuzzleHelper
{
    [Tracked]
    public class PuzzleFallingBlockCollider : Component
    {
        public Action<PuzzleFallingBlock> OnCollide;

        public Collider Collider;

        public PuzzleFallingBlockCollider(Action<PuzzleFallingBlock> onCollide, Collider collider = null)
            : base(active: false, visible: false)
        {
            OnCollide = onCollide;
            Collider = null;
        }

        public void Check(PuzzleFallingBlock block)
        {
            if (OnCollide != null)
            {
                Collider collider = base.Entity.Collider;
                if (Collider != null)
                {
                    base.Entity.Collider = Collider;
                }

                if (block.CollideCheck(base.Entity))
                {
                    OnCollide(block);
                }

                base.Entity.Collider = collider;
            }
        }
    }
}
