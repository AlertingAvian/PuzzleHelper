using Celeste.Mod.AidenHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
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
            Logger.SetLogLevel("PuzzleHelper", LogLevel.Info);
            Logger.Log(LogLevel.Debug, "PuzzleHelper", "Load");
            On.Monocle.Entity.Awake += modAwake;
            On.Celeste.Solid.HasRider += modHasRider;
            On.Celeste.Actor.MoveVExact += modActorMoveVExact;
            On.Celeste.Actor.MoveHExact += modActorMoveHExact;
            On.Celeste.Solid.MoveHExact += modSolidMoveHExact;
            On.Celeste.Solid.MoveVExact += modSolidMoveVExact;
            On.Celeste.Platform.Update += modPlatformUpdate;
            //On.Celeste.SinkingPlatform.Update += modSinkingPlatformUpdate;
        }

        public override void Unload()
        {
            On.Monocle.Entity.Awake -= modAwake;
            On.Celeste.Solid.HasRider -= modHasRider;
            On.Celeste.Actor.MoveVExact -= modActorMoveVExact;
            On.Celeste.Actor.MoveHExact -= modActorMoveHExact;
            On.Celeste.Solid.MoveHExact -= modSolidMoveHExact;
            On.Celeste.Solid.MoveVExact -= modSolidMoveVExact;
            On.Celeste.Platform.Update -= modPlatformUpdate;
            //On.Celeste.SinkingPlatform.Update -= modSinkingPlatformUpdate;

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
            self.GetRiders();
            FieldInfo field = typeof(Solid).GetField("riders", BindingFlags.NonPublic
                                                             | BindingFlags.Instance
                                                             | BindingFlags.Static);
            HashSet<Actor> riders = field.GetValue(self) as HashSet<Actor>;
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
            self.GetRiders();
            FieldInfo field = typeof(Solid).GetField("riders", BindingFlags.NonPublic
                                                             | BindingFlags.Instance
                                                             | BindingFlags.Static);
            HashSet<Actor> riders = field.GetValue(self) as HashSet<Actor>;
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
                    }                                      // to here
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

        private void modPlatformUpdate(On.Celeste.Platform.orig_Update orig, Platform self)
        {
            if (self is SinkingPlatform sinkingPlatform)
            {
                FieldInfo riseTimerfield = typeof(SinkingPlatform).GetField("riseTimer", BindingFlags.NonPublic
                                                                          | BindingFlags.Instance);
                float? riseTimer = riseTimerfield.GetValue(sinkingPlatform) as float?;

                FieldInfo startYfield = typeof(SinkingPlatform).GetField("startY", BindingFlags.NonPublic
                                                                 | BindingFlags.Instance);
                float? startY = startYfield.GetValue(sinkingPlatform) as float?;

                FieldInfo shakerfield = typeof(SinkingPlatform).GetField("shaker", BindingFlags.NonPublic
                                                                 | BindingFlags.Instance);
                Shaker shaker = shakerfield.GetValue(sinkingPlatform) as Shaker;

                FieldInfo speedfield = typeof(SinkingPlatform).GetField("speed", BindingFlags.NonPublic
                                                                | BindingFlags.Instance);
                float? speed = speedfield.GetValue(sinkingPlatform) as float?;

                if (riseTimer == null)
                {
                    Logger.Log(LogLevel.Error, "PuzzleHelper", "riseTimer was null");
                }

                if (startY == null)
                {
                    Logger.Log(LogLevel.Error, "PuzzleHelper", "startY was null");
                }

                if (speed == null)
                {
                    Logger.Log(LogLevel.Error, "PuzzleHelper", "speed was null");
                }

                PuzzleFallingBlock blockRider = null;
                foreach (PuzzleFallingBlock block in sinkingPlatform.Scene.Tracker.GetEntities<PuzzleFallingBlock>())
                {
                    if (block.IsRiding(sinkingPlatform))
                    {
                        blockRider = block;
                        break;
                    }
                }
                if (blockRider != null)
                {
                    if (riseTimer <= 0f)
                    {
                        if (sinkingPlatform.ExactPosition.Y <= startY)
                        {
                            Audio.Play("event:/game/03_resort/platform_vert_start", sinkingPlatform.Position);
                        }

                        shaker.ShakeFor(0.15f, removeOnFinish: false);
                    }

                    riseTimer = 0.1f;
                    riseTimerfield.SetValue(sinkingPlatform, riseTimer);

                    speed = Calc.Approach(speed.Value, 30f, 400f * Engine.DeltaTime);
                    speedfield.SetValue(sinkingPlatform, speed);
                }
            } 
            else if (self is LinkedSinkingPlatform linkedSinkingPlatform)
            {
                // sorry for the amount of reflection you are about to see
                FieldInfo startYField = typeof(LinkedSinkingPlatform).GetField("startY", BindingFlags.NonPublic
                                                                                       | BindingFlags.Instance);

                FieldInfo endYField = typeof(LinkedSinkingPlatform).GetField("endY", BindingFlags.NonPublic
                                                                                   | BindingFlags.Instance);

                FieldInfo shakerField = typeof(LinkedSinkingPlatform).GetField("shaker", BindingFlags.NonPublic
                                                                                       | BindingFlags.Instance);

                FieldInfo speedField = typeof(LinkedSinkingPlatform).GetField("speed", BindingFlags.NonPublic
                                                                                     | BindingFlags.Instance);

                FieldInfo reversedField = typeof(LinkedSinkingPlatform).GetField("reversed", BindingFlags.NonPublic
                                                                                           | BindingFlags.Instance);

                FieldInfo enabledField = typeof(LinkedSinkingPlatform).GetField("enabled", BindingFlags.NonPublic
                                                                                         | BindingFlags.Instance);

                FieldInfo riseTimerField = typeof(LinkedSinkingPlatform).GetField("riseTimer", BindingFlags.NonPublic
                                                                                             | BindingFlags.Instance);

                FieldInfo downTimerField = typeof(LinkedSinkingPlatform).GetField("downTimer", BindingFlags.NonPublic
                                                                                             | BindingFlags.Instance);

                FieldInfo downSFXEnabledField = typeof(LinkedSinkingPlatform).GetField("downSFXEnabled", BindingFlags.NonPublic
                                                                                                       | BindingFlags.Instance);

                FieldInfo upSFXEnabledField = typeof(LinkedSinkingPlatform).GetField("upSFXEnabled", BindingFlags.NonPublic
                                                                                                   | BindingFlags.Instance);

                FieldInfo downSfxField = typeof(LinkedSinkingPlatform).GetField("downSfx", BindingFlags.NonPublic
                                                                                         | BindingFlags.Instance);

                FieldInfo upSfxField = typeof(LinkedSinkingPlatform).GetField("upSfx", BindingFlags.NonPublic
                                                                                     | BindingFlags.Instance);
                // THERE MAY BE ISSUES HERE WITH NULL VALUES

                float? startY = startYField.GetValue(linkedSinkingPlatform) as float?;
                float? endY = endYField.GetValue(linkedSinkingPlatform) as float?;
                Shaker shaker = shakerField.GetValue(linkedSinkingPlatform) as Shaker; // cannot be null
                float? speed = speedField.GetValue(linkedSinkingPlatform) as float?;
                bool? reversed = reversedField.GetValue(linkedSinkingPlatform) as bool?;
                bool? enabled = enabledField.GetValue(linkedSinkingPlatform) as bool?;
                float? riseTimer = riseTimerField.GetValue(linkedSinkingPlatform) as float?;
                float? downTimer = downTimerField.GetValue(linkedSinkingPlatform) as float?;
                bool? downSFXEnabled = downSFXEnabledField.GetValue(linkedSinkingPlatform) as bool?;
                bool? upSFXEnabled = upSFXEnabledField.GetValue(linkedSinkingPlatform) as bool?;
                SoundSource downSfx = downSfxField.GetValue(linkedSinkingPlatform) as SoundSource; // cannot be null
                SoundSource upSfx = upSfxField.GetValue(linkedSinkingPlatform) as SoundSource; // cannot be null

                // end of reflection monster. start of update

                PuzzleFallingBlock blockRider = null;
                foreach (PuzzleFallingBlock block in linkedSinkingPlatform.Scene.Tracker.GetEntities<PuzzleFallingBlock>())
                {
                    if (block.IsRiding(linkedSinkingPlatform))
                    {
                        blockRider = block;
                        break;
                    }
                }

                if (blockRider != null && !linkedSinkingPlatform.MasterOfGroup && !reversed.Value)
                {
                    foreach (LinkedSinkingPlatform item in linkedSinkingPlatform.Group)
                    {
                        float masterDiff = endY.Value - startY.Value;
                        // item.Move happenes here (more reflection)
                        float? itemStartY = startYField.GetValue(item) as float?;
                        float? itemEndY = endYField.GetValue(item) as float?;
                        Shaker itemShaker = shakerField.GetValue(item) as Shaker; // cannot be null
                        float? itemSpeed = speedField.GetValue(item) as float?;
                        bool? itemReversed = reversedField.GetValue(item) as bool?;
                        bool? itemEnabled = enabledField.GetValue(item) as bool?;
                        float? itemRiseTimer = riseTimerField.GetValue(item) as float?;
                        float? itemDownTimer = downTimerField.GetValue(item) as float?;
                        bool? itemDownSFXEnabled = downSFXEnabledField.GetValue(item) as bool?;
                        bool? itemUpSFXEnabled = upSFXEnabledField.GetValue(item) as bool?;
                        SoundSource itemDownSfx = downSfxField.GetValue(item) as SoundSource; // cannot be null
                        SoundSource itemUpSfx = upSfxField.GetValue(item) as SoundSource; // cannot be null

                        float num = (itemEndY.Value - itemStartY.Value) / masterDiff;

                        if (blockRider != null)
                        {

                            if (itemRiseTimer.Value <= 0f)
                            {
                                if (item.ExactPosition.Y <= itemStartY.Value)
                                {
                                    Audio.Play("event:/game/03_resort/platform_vert_start", item.Position);
                                }

                                itemShaker.ShakeFor(0.15f, removeOnFinish: false);
                            }
                            riseTimerField.SetValue(item, 0.1f);
                            float calcSpeed = Calc.Approach(itemSpeed.Value, (float)(!itemReversed.Value ? 1 : (-1)) * 30f * num, 400f * num * Engine.DeltaTime);
                            speedField.SetValue(item, calcSpeed);
                        }
                        else if (itemRiseTimer.Value > 0f)
                        {
                            riseTimerField.SetValue(item, itemRiseTimer.Value - Engine.DeltaTime);
                            float calcSpeed = Calc.Approach(itemSpeed.Value, (float)((!itemReversed.Value) ? 1 : (-1)) * 45f * num, 600f * num * Engine.DeltaTime);
                            speedField.SetValue(item, calcSpeed);
                        }
                        else
                        {
                            downTimerField.SetValue(item, itemDownTimer.Value - Engine.DeltaTime);
                            float calcSpeed = Calc.Approach(itemSpeed.Value, (float)((!itemReversed.Value) ? 1 : (-1)) * -50f, 400f * num * Engine.DeltaTime);
                            speedField.SetValue(item, calcSpeed);
                        }

                        itemSpeed = speedField.GetValue(item) as float?;
                        // not done with item.move yet
                        
                        // end item.Move
                        item.MasterOfGroup = false;
                        item.master = linkedSinkingPlatform;
                        bool? instanceReversed = reversedField.GetValue(item) as bool?;
                        if (instanceReversed.Value)
                        {
                            enabledField.SetValue(item, true);
                        }
                    }
                    // ^ end of foreach loop
                }
            }
        }

        private void modSinkingPlatformUpdate(On.Celeste.SinkingPlatform.orig_Update orig, SinkingPlatform self)
        {
            FieldInfo riseTimerfield = typeof(SinkingPlatform).GetField("riseTimer", BindingFlags.NonPublic
                                                                          | BindingFlags.Instance);
            float? riseTimer = riseTimerfield.GetValue(self) as float?;

            FieldInfo startYfield = typeof(SinkingPlatform).GetField("startY", BindingFlags.NonPublic
                                                             | BindingFlags.Instance);
            float? startY = startYfield.GetValue(self) as float?;

            FieldInfo shakerfield = typeof(SinkingPlatform).GetField("shaker", BindingFlags.NonPublic
                                                             | BindingFlags.Instance);
            Shaker shaker = shakerfield.GetValue(self) as Shaker;

            FieldInfo speedfield = typeof(SinkingPlatform).GetField("speed", BindingFlags.NonPublic
                                                            | BindingFlags.Instance);
            float? speed = speedfield.GetValue(self) as float?;

            if (riseTimer == null)
            {
                Logger.Log(LogLevel.Error, "PuzzleHelper", "riseTimer was null");
            }

            if (startY == null)
            {
                Logger.Log(LogLevel.Error, "PuzzleHelper", "startY was null");
            }
            
            if (speed == null)
            {
                Logger.Log(LogLevel.Error, "PuzzleHelper", "speed was null");
            }

            PuzzleFallingBlock blockRider = null;
            foreach (PuzzleFallingBlock block in self.Scene.Tracker.GetEntities<PuzzleFallingBlock>())
            {
                if (block.IsRiding(self))
                {
                    blockRider = block;
                    break;
                }
            }
            if (blockRider != null)
            {
                if (riseTimer <= 0f)
                {
                    if (self.ExactPosition.Y <= startY)
                    {
                        Audio.Play("event:/game/03_resort/platform_vert_start", self.Position);
                    }

                    shaker.ShakeFor(0.15f, removeOnFinish: false);
                }

                riseTimer = 0.1f;
                riseTimerfield.SetValue(self, riseTimer);

                speed = Calc.Approach(speed.Value, 30f, 400f * Engine.DeltaTime);
                speedfield.SetValue(self, speed);
            }
            orig(self);
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
