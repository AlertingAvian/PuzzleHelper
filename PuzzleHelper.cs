using Monocle;
using System.Reflection;

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
                spring.Add(puzzleBlockCollider);
            }
            orig(self, scene);
        }

        private void onBlock(PuzzleBlock block, Spring spring)
        {
            if(block.HitSpring(spring))
            {
                typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(spring, null);
            }
            
        }
    }
}
