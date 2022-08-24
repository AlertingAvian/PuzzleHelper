using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using System.Collections;

namespace Celeste.Mod.PuzzleHelper
{
    [Tracked]
    [CustomEntity("PuzzleHelper/PuzzleFallingBlock")] // same as fallingBlock but should land on moving platforms and move with them
    public class PuzzleBlock : FallingBlock
    {
        private Vector2 moveSpeed; // speed modification applied in update
        private Vector2 lastPosition; // last position, updated in update
        private bool thruDashBlocks; // required for movement, don't know what its for
        private bool ignoreJumpThrus; // i have this for some reason, I don't know why
        private float noGravityTimer; // how long before reducing Y speed modifier
        private Vector2 speed; // speed calculated in update because base class Speed doesn't do anything
        private int number; // a number that iterates with update so I don't spam the log file. as much.
        private float baseSpeed; // 
        private float baseMaxSpeed;
        private bool hasNotLanded;

        public PuzzleBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind, bool climbFall, bool IgnoreJumpThrus)
            : base(position, tile, width, height, finalBoss, behind, climbFall)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", "New instance of PuzzleBlock");
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"IngoreJumpThrus: {ignoreJumpThrus}");
            ignoreJumpThrus = IgnoreJumpThrus;
            thruDashBlocks = true; // I just leave this as false for now, if I take the time to figure out what it does I will probably add it as an option in Ahorn
            lastPosition = base.ExactPosition; // setting first lastPosition
            number = 0; // numbers have to start somehwere
            baseSpeed = 0f;
            baseMaxSpeed = finalBoss ? 130f : 160f;
            hasNotLanded = true;

        }

        public PuzzleBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height, finalBoss: false, data.Bool("behind"), data.Bool("climbFall", defaultValue: true), data.Bool("ignoreJumpThrus", defaultValue: false))
        {
        }

        private void onJumpThruCollide(JumpThru jumpThru) // don't know why i have this
        {
            Logger.Log(LogLevel.Debug, "PuzzleHelepr", jumpThru.ToString()); // pretty sure i dont need to do anything here
        }

        public override void Update() // refactor this to be a little bit more DRY
        {
            base.Update();
 
            if (hasNotLanded && HasStartedFalling) // redo all of this
            {
                baseSpeed = Calc.Approach(baseSpeed, baseMaxSpeed, 500f * Engine.DeltaTime);
                MoveVCollideSolids((baseSpeed * Engine.DeltaTime) * -1, thruDashBlocks);
                if(MoveVCollideSolids((baseSpeed * 1.01f) * Engine.DeltaTime, thruDashBlocks, OnCollideV))
                {
                    hasNotLanded = false;
                }
            }
            

            // calculate speed
            speed.X = (ExactPosition.X - lastPosition.X) / Engine.DeltaTime;
            speed.Y = (ExactPosition.Y - lastPosition.Y) / Engine.DeltaTime;

            // spam log useful information
            if (number % 10 == 0)
            {
                Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"Gravity Timer: {noGravityTimer}, Speed: {speed}, Speed Modifier: {moveSpeed}");
            }
            number++;

            lastPosition = base.ExactPosition; // save last position (ONLY AFTER speed IS CALCULATED)

            foreach (PuzzleBlockCollider component in base.Scene.Tracker.GetComponents<PuzzleBlockCollider>()) // run check on all of the Colliders in the scene
            {
                component.Check(this);
            }

            if (noGravityTimer > 0f)
            {
                noGravityTimer -= Engine.DeltaTime;
                if (noGravityTimer < 0f) // don't want negative time i guess
                {
                    noGravityTimer = 0f;
                }

            }
            else if (noGravityTimer == 0f) // start reducing Y speed when timer runs out
            {
                if (moveSpeed.Y > 0f) // there is a better way to do this, the amount by which you change the speed MUST be a multiple of the set speed or you will end up in a loop adding and subtracting
                {
                    moveSpeed.Y -= 5f;
                }
                else if (moveSpeed.Y < 0f)
                {
                    moveSpeed.Y += 5f;
                }
            }

            MoveHCollideSolids(moveSpeed.X * Engine.DeltaTime, thruDashBlocks, OnCollideH); // apply calculated movements
            MoveVCollideSolids(moveSpeed.Y * Engine.DeltaTime, thruDashBlocks, OnCollideV); // im pretty sure thruDashBlocks will just make it break the blocks if this is doing the movement, set to false for now.
            if (moveSpeed.X != 0f)
            /*
             * reduce the X speed
             * because this will only have a value assigned to it when it hits the spring there doesn't need to be any other checks
             * has the same issue as the Y speed reduction
             * must be a multiple of the set speed...
             */
            {
                if (moveSpeed.X > 0f)
                {
                    moveSpeed.X -= 5f;
                }
                else if (moveSpeed.X < 0f)
                {
                    moveSpeed.X += 5f;
                }
            }
        }

        private void OnCollideH(Vector2 x1, Vector2 x2, Platform platform)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"OnCollideH: {x1}, {x2}, {platform}"); // actually useful information to see what the collisions are will only be called if the the move in this update is doing the moving though
        }

        private void OnCollideV(Vector2 x1, Vector2 x2, Platform platform)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"OnCollideV: {x1}, {x2}, {platform}"); // see above comment
            if(platform is JumpThru)
            {
                Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"platform {platform} is JumpThru");
                Add(new Coroutine(jumpThruMove(platform as JumpThru)));
            }
        }

        private IEnumerator jumpThruMove(JumpThru jumpThru)
        {
            Vector2 jumpThruSpeed = Vector2.Zero;
            Vector2 jumpThruLastPosition = jumpThru.ExactPosition;
            while(true)
            {
                jumpThruSpeed.X = (jumpThru.ExactPosition.X - jumpThruLastPosition.X) / Engine.DeltaTime;
                jumpThruSpeed.Y = (jumpThru.ExactPosition.Y - jumpThruLastPosition.Y) / Engine.DeltaTime;
                jumpThruLastPosition = jumpThru.ExactPosition;
                MoveH(jumpThruSpeed.X * Engine.DeltaTime);
                MoveV(jumpThruSpeed.Y * Engine.DeltaTime);
                yield return null;
            }
        }

        public bool IsRiding(JumpThru jumpThru)
        {
            if (ignoreJumpThrus)
            {
                return false;
            }
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"{this} is riding jumpthru");
            return CollideCheckOutside(jumpThru, Position + Vector2.UnitY);
        }

        public bool IsRiding(Solid solid)
        {
            Logger.Log(LogLevel.Verbose, "PuzzleHelper", $"{this} is riding solid");
            return CollideCheck(solid, Position + Vector2.UnitY);
        }

        public bool HitSpring(Spring spring) // called by spring when it hits a spring.
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
                case Spring.Orientations.WallLeft: // block has no negative y movement when you hit a wall spring at the moment, was getting weird behaviour when i was trying it
                    if (Speed.X <= 60f)
                    {
                        moveSpeed.X = 300f;
                        return true;
                    }

                    return false;
                case Spring.Orientations.WallRight:
                    if (Speed.X >= -60f)
                    {
                        moveSpeed.X = -300f;
                        return true;
                    }

                    return false;
            }
        }

        public void HitDashSwitch(DashSwitch dashSwitch)
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
}