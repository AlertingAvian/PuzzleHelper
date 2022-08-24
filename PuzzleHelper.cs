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
            On.Celeste.JumpThru.HasRider += modJumpThruHasRider;
            On.Celeste.Solid.HasRider += modSolidHasRider;
        }

        public override void Unload()
        {
            On.Monocle.Entity.Awake -= modAwake;
            On.Celeste.JumpThru.HasRider -= modJumpThruHasRider;
            On.Celeste.Solid.HasRider -= modSolidHasRider;
        }

        private void modAwake(On.Monocle.Entity.orig_Awake orig, Entity self, Scene scene)
        {
            if (self is Spring spring)
            {
                PuzzleBlockCollider puzzleBlockCollider = new PuzzleBlockCollider((block) => springOnBlock(block, spring));
                spring.Add(puzzleBlockCollider);
            }

            if (self is DashSwitch dashSwitch)
            {
                Logger.Log(LogLevel.Verbose, "PuzzleHelper", "Added Collider to DashSwitch");
                PuzzleBlockCollider puzzleBlockCollider = new PuzzleBlockCollider((block) => dashSwitchOnBlock(block, dashSwitch));
                FieldInfo field = typeof(DashSwitch).GetField("side", BindingFlags.NonPublic
                                                                    | BindingFlags.Instance);
                DashSwitch.Sides? side = field.GetValue(dashSwitch) as DashSwitch.Sides?;
                if (side == null)
                {
                    Logger.Log(LogLevel.Warn, "PuzzleHelper", "Unable to retrive DashSwitch side");
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"{side}");
                    switch (side)
                    {
                        case DashSwitch.Sides.Left:
                            puzzleBlockCollider.Collider = new Hitbox(10f, 16f, 2f, 0f);
                            break;
                        case DashSwitch.Sides.Right:
                            puzzleBlockCollider.Collider = new Hitbox(10f, 16f, -2f, 0f);
                            break;
                        case DashSwitch.Sides.Up:
                            puzzleBlockCollider.Collider = new Hitbox(16f, 10f, 0f, 2f);
                            break;
                        case DashSwitch.Sides.Down:
                            puzzleBlockCollider.Collider = new Hitbox(16f, 10f, 0f, -2f);
                            break;
                    }
                }
                dashSwitch.Add(puzzleBlockCollider);
            }
            orig(self, scene);
        } 

        //private void modSolidGetRiders(On.Celeste.Solid.orig_GetRiders orig, Solid self)
        //{

        //    foreach (PuzzleBlock entity in self.Scene.Tracker.GetEntities<PuzzleBlock>())
        //    {
        //        FieldInfo field = typeof(Solid).GetField("riders", BindingFlags.Instance 
        //                                                         | BindingFlags.NonPublic);
        //        if (entity.IsRiding(self))
        //        {
        //            riders.Add(entity); // needs modification access to private field
        //        }
        //    }
        //    orig(self);
        //}

        private bool modJumpThruHasRider(On.Celeste.JumpThru.orig_HasRider orig, JumpThru self)
        {
            foreach (PuzzleBlock entity in self.Scene.Tracker.GetEntities<PuzzleBlock>())
            {
                if (entity.IsRiding(self))
                {
                    Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"{self} has puzzleblock rider");
                    return true;
                }
            }
            return orig(self);
        }

        private bool modSolidHasRider(On.Celeste.Solid.orig_HasRider orig, Solid self)
        {
            foreach (PuzzleBlock entity in self.Scene.Tracker.GetEntities<PuzzleBlock>())
            {
                if (entity.IsRiding(self))
                {
                    Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"{self} has puzzleblock rider");
                    return true;
                }
            }
            return orig(self);
        }


        private void springOnBlock(PuzzleBlock block, Spring spring)
        {
            if (block.HitSpring(spring))
            {
                typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic
                                                        | BindingFlags.Instance).Invoke(spring, null);
            }

        }

        private void dashSwitchOnBlock(PuzzleBlock block, DashSwitch dashSwitch)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", "dashSwitchOnBlock");
            block.HitDashSwitch(dashSwitch);
        }
    }
}
