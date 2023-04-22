using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using TerraQuake;

namespace TerraQuake
{
    internal class Ghost : Component
    {
        public Renderer RendererBody = null;
        public Vector2 Velocity = new Vector2(0, 0);
        public Vector2 KnockbackVelocity = new Vector2(0,0);
        public Vector2 ColisionBounds = new Vector2(22, 30);
        public Vector2 ColisionOffset = new Vector2(9, 4);
        public Animator AnimatorBody = null;
        public TimeSpan NextBlink = TimeSpan.Zero;
        public TimeSpan NextBob = TimeSpan.Zero;
        public TimeSpan StartIdleTime = TimeSpan.FromSeconds(10);
        public bool Side = true;
        public static List<Ghost> Ghosts = new List<Ghost>();
        public int InsertAmmo = 0;
        public TimeSpan LastUpdate = TimeSpan.Zero;
        public bool OnGround = false;
        public bool RightBlocked = false;
        public bool LeftBlocked = false;
        public int ClimbHeigh = 8;
        public int LightRadius = 120;

        public int CanClimb(Microsoft.Xna.Framework.Rectangle Colision, int Side, int X)
        {
            int HowMuch = 0;
            Terrain Terra = ContentManager.Game.TerrainInstance;
            for (int i = 1; i < ClimbHeigh; i++)
            {
                if(Colision.Bottom - i >= 0)
                {
                    if (!Terra.GetPixel(Side, Colision.Bottom - i).IsAir())
                    {
                        HowMuch++;
                    }
                } else
                {
                    HowMuch++;
                    break;
                }
            }

            Microsoft.Xna.Framework.Rectangle FutureColision = new Microsoft.Xna.Framework.Rectangle(Colision.X + X, Colision.Y-HowMuch, Colision.Width, Colision.Height);
            if (Terra.CheckCollision(FutureColision))
            {
                HowMuch = 0;
            }

            return HowMuch;
        }

        public void Moved()
        {

        }
        public void PreMoved()
        {

        }

        public override void Update(GameTime gameTime)
        {
            LastUpdate = gameTime.TotalGameTime;
            LayersManager.ScrollTo(Object.Position);
            Microsoft.Xna.Framework.Rectangle Colision = GetPhysicalColision2();
            Microsoft.Xna.Framework.Rectangle Tail = new Microsoft.Xna.Framework.Rectangle(Colision.Left, Colision.Bottom, Colision.Width, 1);
            Microsoft.Xna.Framework.Rectangle RightSide = new Microsoft.Xna.Framework.Rectangle(Colision.Right+1, Colision.Top, 1, Colision.Height);
            Microsoft.Xna.Framework.Rectangle LeftSide = new Microsoft.Xna.Framework.Rectangle(Colision.Left-1, Colision.Top, 1, Colision.Height);
            OnGround = ContentManager.Game.TerrainInstance.CheckCollision(Tail);
            RightBlocked = ContentManager.Game.TerrainInstance.CheckCollision(RightSide);
            LeftBlocked = ContentManager.Game.TerrainInstance.CheckCollision(LeftSide);

            RendererBody.Scale = 0.5f;

            if (OnGround)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    if (!LeftBlocked)
                    {
                        PreMoved();
                        Object.Position.X--;
                        Moved();
                    } else if (OnGround)
                    {
                        int HowMuch = CanClimb(Colision, Colision.Left - 1, -1);
                        if (HowMuch > 0)
                        {
                            PreMoved();
                            Object.Position.X--;
                            Object.Position.Y -= HowMuch;
                            Moved();
                        }
                    }
                }

                if (Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    if (!RightBlocked)
                    {
                        PreMoved();
                        Object.Position.X++;
                        Moved();
                    } else if (OnGround)
                    {
                        int HowMuch = CanClimb(Colision, Colision.Right + 1, 1);
                        if (HowMuch > 0)
                        {
                            PreMoved();
                            Object.Position.X++;
                            Object.Position.Y -= HowMuch;
                            Moved();
                        }
                    }
                }
            }

            if (Velocity.Y != 0)
            {
                PreMoved();
                Velocity.Y--;
                Object.Position.Y--;
                Moved();
            }

            if (!OnGround)
            {
                if(Velocity.Y == 0)
                {
                    PreMoved();
                    Object.Position.Y++;
                    Moved();
                }
                if (Velocity.X != 0)
                {
                    PreMoved();
                    Object.Position.X++;
                    Velocity.X--;
                    Moved();
                }
            } else
            {
                Velocity.X = 0;
            }

            if (Input.KeyPressed(Keys.K))
            {
                PreMoved();
                Vector2 P = ContentManager.Game.GetPointer();
                Object.Position = P;
                Moved();
            }
        }

        public void Jump()
        {
            if (OnGround)
            {
                Velocity.Y = 30;
            }
        }
        public void JumpForward()
        {
            if (OnGround)
            {
                Velocity.Y = 20;
                Velocity.X = 50;
            }
        }
        public string GetIdle()
        {
            if (Side)
            {
                return "IdleSide";
            } else
            {
                return "Idle";
            }
        }

        public override void OnAttached()
        {
            Ghosts.Add(this);
            AnimatorBody.Animations = GetAnimationsBody();
            AnimatorBody.PlayAnimation(GetIdle());
        }
        public override void OnDestroy()
        {
            Ghosts.Remove(this);
        }

        public Dictionary<string, Animator.AnimationData> GetAnimationsBody()
        {
            Dictionary<string, Animator.AnimationData> Dict = new Dictionary<string, Animator.AnimationData>();

            Animator.AnimationData IdleAnim = new Animator.AnimationData("Idle");
            IdleAnim.DefaultLoop = true;
            Animator.AnimationData IdleSideAnim = new Animator.AnimationData("IdleSide");
            IdleSideAnim.DefaultLoop = true;
            Animator.AnimationData BlinkAnim = new Animator.AnimationData("Blink");
            Animator.AnimationData BlinkSideAnim = new Animator.AnimationData("BlinkSide");
            Animator.AnimationData ShotAnim = new Animator.AnimationData("Shot");
            Animator.AnimationData ShotSideAnim = new Animator.AnimationData("ShotSide");
            Texture2D SpriteSheet = ContentManager.GetSprite("Ghost");
            for (int i = 0; i <= 0; i++) // Idle
            {
                IdleAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * i, 0, 64, 80), 200);
            }
            Dict.Add(IdleAnim.Name, IdleAnim);
            for (int i = 5; i <= 5; i++) // IdleSide
            {
                IdleSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * i, 0, 64, 80), 200);
            }
            Dict.Add(IdleSideAnim.Name, IdleSideAnim);

            BlinkAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 0, 0, 64, 80), 100);
            BlinkAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 1, 0, 64, 80), 100);
            BlinkAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 2, 0, 64, 80), 100);
            BlinkAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 3, 0, 64, 80), 100);
            BlinkAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 2, 0, 64, 80), 100);
            BlinkAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 1, 0, 64, 80), 100);
            BlinkAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 0, 0, 64, 80), 100);
            Dict.Add(BlinkAnim.Name, BlinkAnim);

            BlinkSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 5, 0, 64, 80), 100);
            BlinkSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 6, 0, 64, 80), 100);
            BlinkSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 7, 0, 64, 80), 100);
            BlinkSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 7, 0, 64, 80), 100);
            BlinkSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 6, 0, 64, 80), 100);
            BlinkSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 5, 0, 64, 80), 100);
            Dict.Add(BlinkSideAnim.Name, BlinkSideAnim);

            ShotAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 4, 0, 64, 80), 300);
            ShotAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 2, 0, 64, 80), 70);
            ShotAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 1, 0, 64, 80), 70);
            ShotAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 0, 0, 64, 80), 70);
            Dict.Add(ShotAnim.Name, ShotAnim);

            ShotSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 9, 0, 64, 80), 300);
            ShotSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 7, 0, 64, 80), 70);
            ShotSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 6, 0, 64, 80), 70);
            ShotSideAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(64 * 5, 0, 64, 80), 70);
            Dict.Add(ShotSideAnim.Name, ShotSideAnim);
            return Dict;
        }

        public RectangleF GetPhysicalColision()
        {
            Vector2 ColOffset = ColisionOffset * LayersManager.Scaler;
            Vector2 TempBound = ColisionBounds;
            if (Object != null)
            {
                return new RectangleF(Object.Position.X + ColOffset.X, Object.Position.Y + ColOffset.Y, TempBound.X, TempBound.Y);
            }
            return new RectangleF(0,0,0,0);
        }
        public Microsoft.Xna.Framework.Rectangle GetPhysicalColision2()
        {
            Vector2 ColOffset = ColisionOffset;
            Vector2 TempBound = ColisionBounds;
            int ColOffsetX = (int)ColOffset.X;
            int ColOffsetY = (int)ColOffset.Y;
            int TempBoundX = (int)TempBound.X;
            int TempBoundY = (int)TempBound.Y;
            int PositionX = (int)Object.Position.X;
            int PositionY = (int)Object.Position.Y;
            if (Object != null)
            {
                return new Microsoft.Xna.Framework.Rectangle(PositionX + ColOffsetX, PositionY + ColOffsetY, TempBoundX, TempBoundY);
            }
            return new Microsoft.Xna.Framework.Rectangle(0, 0, 0, 0);
        }

        public override void OnAnimationFinished(GameTime gameTime, string AnimationName, string SenderName)
        {

        }
    }
}
