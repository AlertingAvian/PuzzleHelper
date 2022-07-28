using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.PuzzleHelper
{
    [CustomEntity("PuzzleHelper/PuzzleFallingBlock")] // same as fallingBlock but should land on moving platforms and move with them
    public class PuzzleBlock : FallingBlock
    {
        private Vector2 moveSpeed;
        private Vector2 lastPosition;
        private bool thruDashBlocks;
        private bool ignoreJumpThrus;
        private float noGravityTimer;
        private Vector2 speed;
        private int number;

        public PuzzleBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind, bool climbFall, bool IgnoreJumpThrus)
            : base(position, tile, width, height, finalBoss, behind, climbFall)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", "New instance of PuzzleBlock");
            ignoreJumpThrus = IgnoreJumpThrus;
            thruDashBlocks = false;
            lastPosition = base.ExactPosition;
            number = 0;

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

            speed.X = (ExactPosition.X - lastPosition.X) / Engine.DeltaTime;
            speed.Y = (ExactPosition.Y - lastPosition.Y) / Engine.DeltaTime;

            if (number == 10)
            {
                Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"Gravity Timer: {noGravityTimer}, Speed: {speed}, Speed Modifier: {moveSpeed}");
                number = 0;
            }
            number++;
            
            lastPosition = base.ExactPosition;

            foreach (PuzzleBlockCollider component in base.Scene.Tracker.GetComponents<PuzzleBlockCollider>())
            {
                component.Check(this);
            }

            if (noGravityTimer > 0f)
            {
                noGravityTimer -= Engine.DeltaTime;
                if (noGravityTimer < 0f)
                {
                    noGravityTimer = 0f;
                }
                
            }
            else if (noGravityTimer == 0f)
            {
                if (moveSpeed.Y > 0f)
                {
                    moveSpeed.Y -= 5f;
                }
                else if (moveSpeed.Y < 0f)
                {
                    moveSpeed.Y += 5f;
                }
            }
            
            MoveHCollideSolids(moveSpeed.X * Engine.DeltaTime, thruDashBlocks, OnCollideH);
            MoveVCollideSolids(moveSpeed.Y * Engine.DeltaTime, thruDashBlocks, OnCollideV);
            if(moveSpeed.X != 0f)
            {
                if(moveSpeed.X > 0f)
                {
                    moveSpeed.X -= 5f;
                } else if(moveSpeed.X < 0f)
                {
                    moveSpeed.X += 5f;
                }
            }

        }

        private void OnCollideH(Vector2 x1, Vector2 x2, Platform platform)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"OnCollideH: {x1}, {x2}, {platform}");
            // no idea what either of these are supposed to do but i needed them for the move in update
        }

        private void OnCollideV(Vector2 x1, Vector2 x2, Platform platform)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"OnCollideV: {x1}, {x2}, {platform}");
            // no idea what either of these are supposed to do but i needed them for the move in update
        }

        public bool HitSpring(Spring spring)
        {
            switch (spring.Orientation)
            {
                default:
                    if (Speed.Y >= 0f)
                    {
                        noGravityTimer = 0.15f;
                        moveSpeed.Y = -300;
                        return true;
                    }

                    return false;
                case Spring.Orientations.WallLeft:
                    if (Speed.X <= 60f)
                    {
                        //noGravityTimer = 1f;
                        moveSpeed.X = 300f;
                        return true;
                    }

                    return false;
                case Spring.Orientations.WallRight:
                    if (Speed.X >= -60f)
                    {
                        //noGravityTimer = 1f;
                        moveSpeed.X = -300f;
                        return true;
                    }

                    return false;
            }
        }
    }
}