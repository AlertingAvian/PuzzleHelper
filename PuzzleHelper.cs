using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.PuzzleHelper
{
    public class PuzzleHelper : EverestModule
    {
        public static PuzzleHelper Instance;

        public PuzzleHelper()
        {
            Instance = this;
        }

        public override void Load()
        {
            Logger.SetLogLevel("PuzzleHelper", LogLevel.Verbose);
            Logger.Log(LogLevel.Debug, "PuzzleHelper", "Load");
            On.Monocle.Entity.Awake += modAwake;
        }

        public override void Unload()
        {
            On.Monocle.Entity.Awake -= modAwake;
        }

        private void modAwake(On.Monocle.Entity.orig_Awake orig, Entity self, Scene scene)
        {
            if(self is Spring spring)
            {
                PuzzleBlockCollider puzzleBlockCollider = new PuzzleBlockCollider((block) => onBlock(block, spring));
                Logger.Log(LogLevel.Debug, "PuzzleHelper", spring.Orientation.ToString());
                switch (spring.Orientation)
                {
                    case Spring.Orientations.Floor:
                        puzzleBlockCollider.Collider = new Hitbox(12f, 16f, -8f, -10f);
                        break;
                    case Spring.Orientations.WallLeft:
                        puzzleBlockCollider.Collider = new Hitbox(12f, 16f, 0f, -8f);
                        break;
                    case Spring.Orientations.WallRight:
                        puzzleBlockCollider.Collider = new Hitbox(12f, 16f, -12f, -8f);
                        break;
                }
                Logger.Log(LogLevel.Debug, "PuzzleHelper", puzzleBlockCollider.OnCollide.ToString());
               spring.Add(puzzleBlockCollider);
            }
            orig(self, scene);
        }

        private void onBlock(PuzzleBlock block, Spring spring)
        {
            block.HitSpring(spring);
        }
    }
}
