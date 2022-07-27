using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.PuzzleHelper
{
    [CustomEntity("PuzzleHelper/PuzzleFallingBlock")] // same as fallingBlock but should land on moving platforms and move with them
    public class PuzzleBlock : FallingBlock
    {
        private Vector2 speed;
        private Vector2 moveSpeed;
        private Vector2 lastPosition;
        private bool thruDashBlocks;
        private bool ignoreJumpThrus;
        private float noGravityTimer;

        public PuzzleBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind, bool climbFall, bool IgnoreJumpThrus)
            : base(position, tile, width, height, finalBoss, behind, climbFall)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", "New instance of PuzzleBlock");
            ignoreJumpThrus = IgnoreJumpThrus;
            thruDashBlocks = false;
            lastPosition = base.ExactPosition;

        }

        public PuzzleBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height, finalBoss: false, data.Bool("behind"), data.Bool("climbFall", defaultValue: true), data.Bool("ignoreJumpThrus", defaultValue: false))
        {
        }

        private void onJumpThruCollide(JumpThru jumpThru)
        {
            Logger.Log(LogLevel.Debug, "PuzzleHelepr", jumpThru.ToString()); // pretty sure i dont need to do anything here
        }

        public override void Update()
        {
            base.Update();
            
            lastPosition = base.ExactPosition;

            foreach (PuzzleBlockCollider component in base.Scene.Tracker.GetComponents<PuzzleBlockCollider>())
            {
                component.Check(this);
            }
            

            MoveHCollideSolids(moveSpeed.X * Engine.DeltaTime, thruDashBlocks, OnCollideH);
            MoveVCollideSolids(moveSpeed.Y * Engine.DeltaTime, thruDashBlocks, OnCollideV);

        }

        private void OnCollideH(Vector2 x1, Vector2 x2, Platform platform)
        { 
        }

        private void OnCollideV(Vector2 x1, Vector2 x2, Platform platform)
        {
        }

        public bool HitSpring(Spring spring)
        {
            switch (spring.Orientation)
            {
                default:
                    if (Speed.Y >= 0f)
                    {
                        //MoveTowardsX(spring.CenterX, 4f);s
                        noGravityTimer = 5f;
                        MoveTowardsY(spring.CenterX, -15f);
                        return true;
                    }

                    return false;
                case Spring.Orientations.WallLeft:
                    if (Speed.X <= 60f)
                    {
                        MoveTowardsY(spring.CenterY, 4f);
                        return true;
                    }

                    return false;
                case Spring.Orientations.WallRight:
                    if (Speed.X >= -60f)
                    {
                        MoveTowardsY(spring.CenterY, 4f);
                        return true;
                    }

                    return false;
            }
        }
    }
}