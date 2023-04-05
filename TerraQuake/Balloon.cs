using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace BalloonInvasion
{
    internal class Balloon : Component
    {
        public Renderer MyRenderer = null;
        public TimeSpan NextFrame = new TimeSpan();
        public TimeSpan NextVelocity = new TimeSpan();
        public Vector2 Velocity = new Vector2(0, 0);
        public Vector2 KnockbackVelocity = new Vector2(0,0);
        public bool GotVelocity = false;
        public Vector2 ColisionBounds = new Vector2(22, 30);
        public Vector2 ColisionOffset = new Vector2(14, 6);
        public float Health = 30;
        public float HealthMax = 30;
        public bool Poped = false;
        public ProgressBar HealthBar = null;
        public Animator MyAnimator = null;

        public static List<Balloon> Balloons = new List<Balloon>();
        public static int MissedBalloons = 0;
        public static int PopedBalloons = 0;
        public static int TotalBalloons = 0;
        public override void OnAttached()
        {
            Balloons.Add(this);
            MyAnimator.Animations = GetAnimations();
            MyAnimator.PlayAnimation("Idle");
            MyAnimator.DebugMode= true;
            TotalBalloons++;
        }
        public override void OnDestroy()
        {
            Balloons.Remove(this);
        }

        public Dictionary<string, Animator.AnimationData> GetAnimations()
        {
            Dictionary<string, Animator.AnimationData> Dict = new Dictionary<string, Animator.AnimationData>();

            Animator.AnimationData IdleAnim = new Animator.AnimationData("Idle");
            IdleAnim.DefaultLoop = true;
            IdleAnim.PinPong = true;
            Animator.AnimationData PopAnim = new Animator.AnimationData("Pop");
            PopAnim.DefaultLoop = false;
            Texture2D SpriteSheet = ContentManager.GetSprite("Balloon");
            for (int i = 0; i <= 3; i++) // Idle
            {
                IdleAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(48 * i, 0, 48, 48), 200);
            }
            Dict.Add(IdleAnim.Name, IdleAnim);
            for (int i = 4; i <= 11; i++) // PopAnim
            {
                PopAnim.AddFrame(SpriteSheet, new Microsoft.Xna.Framework.Rectangle(48 * i, 0, 48, 48), 30);
            }
            Dict.Add(PopAnim.Name, PopAnim);

            return Dict;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Poped)
            {
                if (HealthBar != null)
                {
                    HealthBar.Max = HealthMax;
                    HealthBar.Val = Health;
                    HealthBar.Visible = Health < HealthMax;
                }
            } else
            {
                if(HealthBar != null)
                {
                    HealthBar.Visible = false;
                }
            }

            if (!GotVelocity)
            {
                GotVelocity = true;
                System.Random RNG = new System.Random();

                int Multiplyer = RNG.Next(1, 2);
                bool Left = RNG.NextDouble() < 0.5;
                float X = 0.3f * Multiplyer;
                float Y = -0.3f * Multiplyer;
                if (Left)
                {
                    X = -X;
                }
                Velocity = new Vector2(X, Y);
            }
            if (Object != null)
            {
                if(Object.Position.X < -10 && Velocity.X < 0)
                {
                    Velocity.X = -Velocity.X * 2;
                }
                if (Object.Position.X > ContentManager.Game.SceneWidth - 30 && Velocity.X > 0)
                {
                    Velocity.X = -Velocity.X;
                }
                Object.Position += Velocity + KnockbackVelocity;
            }
            if(KnockbackVelocity.Y > 0)
            {
                KnockbackVelocity.Y -= 0.018f;
                if(KnockbackVelocity.Y < 0) 
                {
                    KnockbackVelocity.Y = 0;
                }
            }

            if(Object.Position.Y < -ColisionBounds.Y - 7)
            {
                GameObjectManager.DestroyObject(Object.GUID);
                MissedBalloons++;
            }
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

        public void Hit(float Damage, float Knockback)
        {
            Health -= Damage;
            if(Health <= 0 && !Poped)
            {
                Poped = true;
                MyAnimator.PlayAnimation("Pop");
            }
            KnockbackVelocity.Y += Knockback;
        }

        public override void OnAnimationFinished(GameTime gameTime, string AnimationName, string SenderName)
        {
            if(AnimationName == "Pop")
            {
                GameObjectManager.DestroyObject(Object.GUID);
                PopedBalloons++;
            }
        }
    }
}
