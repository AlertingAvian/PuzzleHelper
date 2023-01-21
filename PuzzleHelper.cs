using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Celeste.DashSwitch;

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
            On.Celeste.Solid.HasRider += modHasRider;
            On.Celeste.Solid.GetRiders += modGetRiders;
            On.Celeste.Actor.MoveVExact += modActorMoveVExact;
            On.Celeste.Actor.MoveHExact += modActorMoveHExact;
            On.Celeste.Solid.MoveHExact += modSolidMoveHExact;
            On.Celeste.Solid.MoveVExact += modSolidMoveVExact;

        }

        public override void Unload()
        {
            On.Monocle.Entity.Awake -= modAwake;
            On.Celeste.Solid.HasRider -= modHasRider;
            On.Celeste.Solid.GetRiders -= modGetRiders;
            On.Celeste.Actor.MoveVExact -= modActorMoveVExact;
            On.Celeste.Actor.MoveHExact -= modActorMoveHExact;
            On.Celeste.Solid.MoveHExact -= modSolidMoveHExact;
            On.Celeste.Solid.MoveVExact -= modSolidMoveVExact;

        }

        private void modGetRiders(On.Celeste.Solid.orig_GetRiders orig, Solid self)
        {
            HashSet<Actor> riders = new HashSet<Actor>();
            if (!(self is PuzzleFallingBlock))
            {
                foreach (PuzzleFallingBlock entity in self.Scene.Tracker.GetEntities<PuzzleFallingBlock>())
                {
                    if (!(self == entity))
                    {
                        if (entity.IsRiding(self as Solid))
                        {
                            riders.Add(entity.Wrapper);
                        }
                    }
                }
            }
            foreach (Actor entity in self.Scene.Tracker.GetEntities<Actor>())
            { 
                if (self is PuzzleFallingBlock && entity is PuzzleFallingBlockActorWrapper)
                {
                    continue;
                }
                if (entity.IsRiding(self as Solid))
                {
                    riders.Add(entity);
                }
            }
            FieldInfo field = typeof(Solid).GetField("riders", BindingFlags.NonPublic
                                                             | BindingFlags.Instance
                                                             | BindingFlags.Static);
            field.SetValue(self, riders);
        }

        private bool modHasRider(On.Celeste.Solid.orig_HasRider orig, Entity self)
        {
            if (!(self is PuzzleFallingBlock))
            {
                foreach (PuzzleFallingBlock entity in self.Scene.Tracker.GetEntities<PuzzleFallingBlock>())
                {
                    if (entity.IsRiding(self as Solid))
                    {
                        return true;
                    }
                }
            }
                
            return orig(self as Solid);
        }

        private void modAwake(On.Monocle.Entity.orig_Awake orig, Entity self, Scene scene)
        {
            if (self is Spring spring)
            {
                PuzzleFallingBlockCollider puzzleFallingBlockCollider = new PuzzleFallingBlockCollider((block) => springOnBlock(block, spring));
                spring.Add(puzzleFallingBlockCollider);
            }

            if (self is DashSwitch dashSwitch)
            {
                PuzzleFallingBlockCollider puzzleFallingBlockCollider = new PuzzleFallingBlockCollider((block) => dashSwitchOnBlock(block, dashSwitch));
                FieldInfo field = typeof(DashSwitch).GetField("side", BindingFlags.NonPublic
                                                                    | BindingFlags.Instance);
                DashSwitch.Sides? side = field.GetValue(dashSwitch) as DashSwitch.Sides?;
                if (side == null)
                {
                    Logger.Log(LogLevel.Warn, "PuzzleHelper", "Unable to retrive DashSwitch side");
                }
                else
                {
                    switch (side)
                    {
                        case DashSwitch.Sides.Left:
                            puzzleFallingBlockCollider.Collider = new Hitbox(10f, 16f, 2f, 0f);
                            break;
                        case DashSwitch.Sides.Right:
                            puzzleFallingBlockCollider.Collider = new Hitbox(10f, 16f, -2f, 0f);
                            break;
                        case DashSwitch.Sides.Up:
                            puzzleFallingBlockCollider.Collider = new Hitbox(16f, 10f, 0f, 2f);
                            break;
                        case DashSwitch.Sides.Down:
                            puzzleFallingBlockCollider.Collider = new Hitbox(16f, 10f, 0f, -2f);
                            break;
                    }
                }
                dashSwitch.Add(puzzleFallingBlockCollider);
            }
            orig(self, scene);
        } 

        private void modSolidMoveHExact(On.Celeste.Solid.orig_MoveHExact orig, Solid self, int move)
        {
            FieldInfo field = typeof(Solid).GetField("riders", BindingFlags.NonPublic
                                                             | BindingFlags.Instance
                                                             | BindingFlags.Static);
            HashSet<Actor> riders = field.GetValue(self) as HashSet<Actor>;
            self.GetRiders();
            float right = self.Right;
            float left = self.Left;
            Player player = null;
            player = self.Scene.Tracker.GetEntity<Player>();
            if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && !riders.Contains(player) && self.CollideCheck(player, self.Position + Vector2.UnitX * move - Vector2.UnitY))
            {
                player.MoveV(1f);
            }

            self.X += move;
            self.MoveStaticMovers(Vector2.UnitX * move);
            if (self.Collidable)
            {
                foreach (Actor entity in self.Scene.Tracker.GetEntities<Actor>())
                {
                    if (self is PuzzleFallingBlock block) // i added from here
                    {
                        if (entity == block.Wrapper)
                        {
                            continue;
                        }
                    }                                   // to here
                    if (!entity.AllowPushing)
                    {
                        continue;
                    }

                    bool collidable = entity.Collidable;
                    entity.Collidable = true;
                    if (!entity.TreatNaive && self.CollideCheck(entity, self.Position))
                    {
                        int moveH = ((move <= 0) ? (move - (int)(entity.Right - left)) : (move - (int)(entity.Left - right)));
                        self.Collidable = false;
                        entity.MoveHExact(moveH, entity.SquishCallback, self);
                        entity.LiftSpeed = self.LiftSpeed;
                        self.Collidable = true;
                    }
                    else if (riders.Contains(entity))
                    {
                        self.Collidable = false;
                        if (entity.TreatNaive)
                        {
                            entity.NaiveMove(Vector2.UnitX * move);
                        }
                        else
                        {
                            entity.MoveHExact(move);
                        }

                        entity.LiftSpeed = self.LiftSpeed;
                        self.Collidable = true;
                    }

                    entity.Collidable = collidable;
                }
            }

            riders.Clear();
        }

        private void modSolidMoveVExact(On.Celeste.Solid.orig_MoveVExact orig, Solid self, int move)
        {
            FieldInfo field = typeof(Solid).GetField("riders", BindingFlags.NonPublic
                                                             | BindingFlags.Instance
                                                             | BindingFlags.Static);
            HashSet<Actor> riders = field.GetValue(self) as HashSet<Actor>;
            self.GetRiders();
            float bottom = self.Bottom;
            float top = self.Top;
            self.Y += move;
            self.MoveStaticMovers(Vector2.UnitY * move);
            if (self.Collidable)
            {
                foreach (Actor entity in self.Scene.Tracker.GetEntities<Actor>())
                {
                    if (self is PuzzleFallingBlock block) // i added from here
                    {
                        if (entity == block.Wrapper)
                        {
                            continue;
                        }
                    }
                    if (!entity.AllowPushing)
                    {
                        continue;
                    }

                    bool collidable = entity.Collidable;
                    entity.Collidable = true;
                    if (!entity.TreatNaive && self.CollideCheck(entity, self.Position))
                    {
                        int moveV = ((move <= 0) ? (move - (int)(entity.Bottom - top)) : (move - (int)(entity.Top - bottom)));
                        self.Collidable = false;
                        entity.MoveVExact(moveV, entity.SquishCallback, self);
                        entity.LiftSpeed = self.LiftSpeed;
                        self.Collidable = true;
                    }
                    else if (riders.Contains(entity))
                    {
                        self.Collidable = false;
                        if (entity.TreatNaive)
                        {
                            entity.NaiveMove(Vector2.UnitY * move);
                        }
                        else
                        {
                            entity.MoveVExact(move);
                        }

                        entity.LiftSpeed = self.LiftSpeed;
                        self.Collidable = true;
                    }

                    entity.Collidable = collidable;
                }
            }

            riders.Clear();
        }

        private bool modActorMoveHExact(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int move, Collision onCollide = null, Solid pusher = null)
        {
            if (self is PuzzleFallingBlockActorWrapper wrapper)
            {
                Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"Move H amount: {move}");
                wrapper.block.MoveHExactCollideSolids(move, thruDashBlocks: true);
                Solid solid = wrapper.CollideFirst<Solid>(wrapper.Position + Vector2.UnitX * Math.Sign(move));
                if (solid != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return orig(self, move, onCollide, pusher);
            }
        }

        private bool modActorMoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int move, Collision onCollide = null, Solid pusher = null)
        {
            if (self is PuzzleFallingBlockActorWrapper wrapper)
            {
                Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"Move V amount: {move}");
                wrapper.block.MoveVExactCollideSolids(move, thruDashBlocks: true);
                Solid solid = wrapper.CollideFirst<Solid>(wrapper.Position + Vector2.UnitX * Math.Sign(move));
                if (solid != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return orig(self, move, onCollide, pusher);
            }
        }

        private void springOnBlock(PuzzleFallingBlock block, Spring spring)
        {
            if (block.HitSpring(spring))
            {
                
            }
        }

        private void dashSwitchOnBlock(PuzzleFallingBlock block, DashSwitch dashSwitch)
        {
            block.HitDashSwitch(dashSwitch);
        }
    }
}
