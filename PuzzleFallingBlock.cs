using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.PuzzleHelper
{
    [Tracked]
    [CustomEntity("PuzzleHelper/PuzzleFallingBlock")]
    public class PuzzleFallingBlock : Solid
    {
        public static ParticleType P_FallDustA => FallingBlock.P_FallDustA;
        public static ParticleType P_FallDustB => FallingBlock.P_FallDustB;
        public static ParticleType P_LandDust => FallingBlock.P_LandDust;

        public bool Triggered;

        public bool TriggerOthers { get; private set; }

        public PuzzleFallingBlockActorWrapper Wrapper { get; private set; }

        public bool OtherTrigger;

        private bool triggerDashSwitches;

        private bool bounceOnSprings;

        private float springHorizontalForce;
        private float springVerticalForce;
        private float springVerticalPercent;

        public float FallDelay;

        public bool HasStartedFalling { get; private set; }

        public bool IgnoreJumpThrus = false; // default to false for now.

        private char tileType;
        private TileGrid tiles;

        private bool climbFall;

        private Vector2 springModifier = Vector2.Zero;

        public PuzzleFallingBlock(EntityData data, Vector2 offset, int width, int height, bool safe, char tile, bool behind, bool climbFall, bool triggerOthers, bool triggerDashSwitches, bool bounceOnSprings, float springHorizontalForce, float springVerticalForce, float springVerticalPercent) : base(data.Position + offset, (float)width, (float)height, safe)
        {
            this.climbFall = climbFall;
            this.TriggerOthers = triggerOthers;
            Hitbox blockTrigger = new Hitbox(Width, 1f, 0, -1);
            PuzzleFallingBlockCollider puzzleFallingBlockCollider = new PuzzleFallingBlockCollider(OnPuzzleFallingBlockCollide);
            puzzleFallingBlockCollider.Collider = blockTrigger;
            Add(puzzleFallingBlockCollider);


            Add(tiles = GFX.FGAutotiler.GenerateBox(tile, width / 8, height / 8).TileGrid);
            tileType = tile;

            Add(new Coroutine(Sequence()));

            Add(new LightOcclude());
            Add(new TileInterceptor(tiles, highPriority: false));

            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tile];

            if (behind)
            {
                base.Depth = 5000;
            }
            this.triggerDashSwitches = triggerDashSwitches;
            this.bounceOnSprings = bounceOnSprings;
            this.springHorizontalForce = springHorizontalForce;
            this.springVerticalForce = springVerticalForce;
            this.springVerticalPercent = springVerticalPercent;
            
            Wrapper = new PuzzleFallingBlockActorWrapper(this);
        }

        public PuzzleFallingBlock(EntityData data, Vector2 offset) : this(data, offset, data.Width, data.Height, data.Bool("safe", true), data.Char("tiletype", '3'), data.Bool("behind", false), data.Bool("climbFall", true), data.Bool("triggerOthers", false), data.Bool("triggerDashSwitches", true), data.Bool("springBounce", true), data.Float("springHorizontal", 120f), data.Float("springVertical"), data.Float("springVerticalPercent", 30f))
        {
            // dont need to do anything here
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            scene.Add(Wrapper);
        }

        public override void Update()
        {
            base.Update();
            foreach (PuzzleFallingBlockCollider component in SceneAs<Level>().Tracker.GetComponents<PuzzleFallingBlockCollider>()) // run check on all of the Colliders in the scene
            {
                if (component.Entity != this)
                {
                    component.Check(this);
                }
            }
            springModifier.X = Calc.Approach(springModifier.X, 0, 300f * Engine.DeltaTime);
            springModifier.Y = Calc.Approach(springModifier.Y, 0, 300f * Engine.DeltaTime);

            Wrapper.Position = Position;
            if (Position.X != Wrapper.Position.X)
            {
                Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"Pos X mismatch -> block: {Position.X}, wrapper: {Wrapper.Position.X}, diff: {Position.X - Wrapper.Position.X}");
            }
            if (Position.Y != Wrapper.Position.Y)
            {
                Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"Pos Y mismatch -> block: {Position.Y}, wrapper: {Wrapper.Position.Y}, diff: {Position.Y - Wrapper.Position.Y}");
            }
        }

        public override void OnShake(Vector2 amount)
        {
            base.OnShake(amount);
            tiles.Position += amount;
        }

        public override void OnStaticMoverTrigger(StaticMover sm)
        {
            Triggered = true;
        }

        private bool PlayerFallCheck()
        {
            if (climbFall)
            {
                return HasPlayerRider();
            }

            return HasPlayerOnTop();
        }

        private bool PlayerWaitCheck()
        {
            if (Triggered)
            {
                return true;
            }
            if (PlayerFallCheck())
            {
                return true;
            }

            if (climbFall)
            {
                if (!CollideCheck<Player>(Position - Vector2.UnitX))
                {
                    return CollideCheck<Player>(Position + Vector2.UnitX);
                }

                return true;
            }

            return false;
        }

        private void OnPuzzleFallingBlockCollide(PuzzleFallingBlock block)
        {
            if (block.HasStartedFalling && block.TriggerOthers)
            {
                this.OtherTrigger = true;
            }
        }

        public virtual bool IsRiding(JumpThru jumpThru)
        {
            if (IgnoreJumpThrus)
            {
                return false;
            }

            return CollideCheckOutside(jumpThru, Position + Vector2.UnitY);
        }

        public virtual bool IsRiding(Solid solid)
        {
            return CollideCheck(solid, Position + Vector2.UnitY);
        }

        private IEnumerator Sequence()
        {
            while (!Triggered && !PlayerFallCheck() && !OtherTrigger)
            {
                yield return null;
            }

            while (FallDelay > 0f)
            {
                FallDelay -= Engine.DeltaTime;
                yield return null;
            }

            HasStartedFalling = true;
            while (true)
            {
                ShakeSfx();
                StartShaking();
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

                yield return 0.2f;
                float timer = 0.4f;

                while (timer > 0f && PlayerWaitCheck())
                {
                    yield return null;
                    timer -= Engine.DeltaTime;
                }

                StopShaking();
                for (int i = 2; (float)i < Width; i += 4)
                {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f)))
                    {
                        SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f, (float)Math.PI / 2f);
                    }

                    SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f);
                }

                float speed = 0f;
                float maxSpeed = 160f;
                float maxMove = 500f;
                while(true)
                {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, maxSpeed, maxMove * Engine.DeltaTime);
                    if (MoveVCollideSolids((speed * Engine.DeltaTime) + (springModifier.Y * Engine.DeltaTime), thruDashBlocks: true))
                    {
                        break;
                    }

                    if (MoveHCollideSolids(springModifier.X * Engine.DeltaTime, thruDashBlocks: true))
                    {

                    }


                    if (Top > (float)(level.Bounds.Bottom + 16) || (Top > (float)(level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + new Vector2(0f, 1f))))
                    {
                        Collidable = Visible = false; // might need () around visible = false
                        yield return 0.2f;
                        if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f)))
                        {
                            yield return 0.2f;
                            SceneAs<Level>().Shake();
                            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                        }

                        RemoveSelf();
                        DestroyStaticMovers();
                        yield break;
                    }

                    yield return null;
                }

                ImpactSfx();
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);

                StartShaking();
                LandParticles();
                yield return 0.2f;
                StopShaking();
                if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f)))
                {
                    break;
                }

                while (CollideCheck<Platform>(Position + new Vector2(0f, 1f)))
                {
                    yield return 0.1f;
                }
            }

            Safe = true;
        }
    
        private void LandParticles()
        {
            for (int i = 2; (float) i <= base.Width; i += 4)
            {
                if (base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f)))
                {
                    SceneAs<Level>().ParticlesFG.Emit(P_FallDustA, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, -(float)Math.PI / 2f);
                    float direction = ((!((float)i < base.Width / 2f)) ? 0f : ((float)Math.PI));
                    SceneAs<Level>().ParticlesFG.Emit(P_LandDust, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, direction);
                }
            }
        }
    
        private void ShakeSfx()
        {
            if (tileType == '3')
            {
                Audio.Play("event:/game/01_forsaken_city/fallblock_ice_shake", base.Center);
            }
            else if (tileType == '9')
            {
                Audio.Play("event:/game/03_resort/fallblock_wood_shake", base.Center);
            }
            else if (tileType == 'g')
            {
                Audio.Play("event:/game/06_reflection/fallblock_boss_shake", base.Center);
            }
            else
            {
                Audio.Play("event:/game/general/fallblock_shake", base.Center);
            }
        }

        private void ImpactSfx()
        {
            if (tileType == '3')
            {
                Audio.Play("event:/game/01_forsaken_city/fallblock_ice_impact", base.BottomCenter);
            }
            else if (tileType == '9')
            {
                Audio.Play("event:/game/03_resort/fallblock_wood_impact", base.BottomCenter);
            }
            else if (tileType == 'g')
            {
                Audio.Play("event:/game/06_reflection/fallblock_boss_impact", base.BottomCenter);
            }
            else
            {
                Audio.Play("event:/game/general/fallblock_impact", base.BottomCenter);
            }
        }

        public void HitDashSwitch(DashSwitch dashSwitch)
        {
            if (triggerDashSwitches)
            {
                Vector2 dir = Vector2.Zero;
                FieldInfo field = typeof(DashSwitch).GetField("side", BindingFlags.NonPublic
                                                                    | BindingFlags.Instance);
                DashSwitch.Sides? side = field.GetValue(dashSwitch) as DashSwitch.Sides?;
                if (side == null)
                {
                    Logger.Log(LogLevel.Warn, "PuzzleHelper", "Unable to retrive DashSwitch side");
                }
                if (HasStartedFalling)
                {
                    switch (side)
                    {
                        case DashSwitch.Sides.Left:
                            dir = new Vector2(-1, 0);
                            break;
                        case DashSwitch.Sides.Right:
                            dir = new Vector2(1, 0);
                            break;
                        case DashSwitch.Sides.Up:
                            dir = new Vector2(0, -1);
                            break;
                        case DashSwitch.Sides.Down:
                            dir = new Vector2(0, 1);
                            break;
                    }
                }
                dashSwitch.OnDashCollide(null, dir);
            }
        }

        public bool HitSpring(Spring spring)
        {
            if (!bounceOnSprings)
            {
                return false;
            }
            switch (spring.Orientation)
            {
                default:
                    if (Speed.Y >= 0f)
                    {
                        typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic
                                                        | BindingFlags.Instance).Invoke(spring, null);
                        springModifier.Y += -springVerticalForce;
                        return true;
                    }

                    return false;
                case Spring.Orientations.WallLeft:
                    if (Speed.X <= 60f)
                    {
                        typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic
                                                        | BindingFlags.Instance).Invoke(spring, null);
                        springModifier.Y += -(springVerticalForce * (springVerticalPercent / 100f));
                        springModifier.X += springHorizontalForce;
                        return true;
                    }

                    return false;
                case Spring.Orientations.WallRight:
                    if (Speed.X >= -60f)
                    {
                        typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic
                                                        | BindingFlags.Instance).Invoke(spring, null);
                        springModifier.Y += -(springVerticalForce * (springVerticalPercent/100f));
                        springModifier.X += -springHorizontalForce;
                        return true;
                    }

                return false;
            }
        }
    }
}
