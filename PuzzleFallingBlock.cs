using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.PuzzleHelper
{
    [Tracked]
    [CustomEntity("PuzzleHelper/PuzzleFallingBlockNEW")]
    public class PuzzleFallingBlock : Solid
    {
        public static ParticleType P_FallDustA => FallingBlock.P_FallDustA;
        public static ParticleType P_FallDustB => FallingBlock.P_FallDustB;
        public static ParticleType P_LandDust => FallingBlock.P_LandDust;

        public bool Triggered;

        public bool TriggerOthers { get; private set; }

        public bool OtherTrigger;

        public float FallDelay;

        public bool HasStartedFalling { get; private set; }

        private char tileType;
        private TileGrid tiles;

        private bool climbFall;

        public PuzzleFallingBlock(EntityData data, Vector2 offset, int width, int height, bool safe, char tile, bool behind, bool climbFall, bool triggerOthers) : base(data.Position + offset, (float)width, (float)height, safe)
        {
            this.climbFall = climbFall;
            this.TriggerOthers = triggerOthers;

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
        }

        public PuzzleFallingBlock(EntityData data, Vector2 offset) : this(data, offset, data.Width, data.Height, data.Bool("safe", true), data.Char("tiletype", '3'), data.Bool("behind", false), data.Bool("climbFall", true), data.Bool("triggerOthers", false))
        {
            // dont need to do anything here
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
                    if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true))
                    {
                        if (TriggerOthers)
                        {
                            PuzzleFallingBlock other = SceneAs<Level>().CollideFirst<PuzzleFallingBlock>(base.BottomCenter + new Vector2(0f, 1f)); // only will trigger the other if it collides with the center of falling one. possible issue.
                            if (other != null)
                            {
                                if (!other.OtherTrigger)
                                {
                                    other.OtherTrigger = true;
                                    continue;
                                }
                            }
                        }
                        
                        break;
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
    }
}
