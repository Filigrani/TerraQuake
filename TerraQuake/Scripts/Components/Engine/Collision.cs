using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class Collision : Component
    {
        public static List<Collision> Coliders = new List<Collision>();
        public int Height = 0;
        public int Widgth = 0;
        public Collision LastColider = null;
        public bool RenderColision = true;
        public bool RenderColisionAdded = false;

        public Collision()
        {
            Coliders.Add(this);
        }
        public override void OnDestroy()
        {
            Coliders.Remove(this);
        }
        public override void OnAttached()
        {
            Object.AddComponent(new DebugRenderer(this));
        }
        
        public void OnColide(Collision other)
        {
            LastColider = other;
        }

        public Rectangle GetCollisionRectangle()
        {
            return new Rectangle((int)Object.Position.X, (int)Object.Position.Y, Height, Widgth);
        }

        public static Vector2 ValidateMovement(GameObject Player, Vector2 Velocity)
        {
            Collision PlayerCol = (Collision)Player.GetComponent(typeof(Collision));
            Vector2 tempVelocity = Velocity;
            foreach (Collision col in Coliders)
            {
                if(PlayerCol != col && !col.Object.IsSleeping())
                {
                    CollisionResult Result;
                    bool Touching = false;

                    if (Velocity.X > 0)
                    {
                        Result = PlayerCol.IsTouchingLeft(col, Velocity.X);
                        
                        if(Result.Touching) 
                        {
                            Touching = true;
                            tempVelocity.X = Result.Vector.X;
                        }
                    }
                    else if (Velocity.X < 0)
                    {
                        Result = PlayerCol.IsTouchingRight(col, Velocity.X);
                        if (Result.Touching) 
                        { 
                            Touching = true;
                            tempVelocity.X = Result.Vector.X;
                        }
                    }

                    if (Velocity.Y < 0)
                    {
                        Result = PlayerCol.IsTouchingBottom(col, Velocity.Y);
                        if (Result.Touching)
                        { 
                            Touching = true;
                            tempVelocity.Y = Result.Vector.Y;
                        }

                    }
                    else if(Velocity.Y > 0)
                    {
                        Result = PlayerCol.IsTouchingTop(col, Velocity.Y);
                        if (Result.Touching) 
                        { 
                            Touching = true;
                            tempVelocity.Y = Result.Vector.Y;
                        }
                    }

                    if (Touching)
                    {
                        PlayerCol.OnColide(col);
                        col.OnColide(PlayerCol);
                    }
                }
            }
            return tempVelocity;
        }

        public struct CollisionResult
        {
            public bool Touching;
            public Vector2 Vector;

            public CollisionResult(bool Touch, Vector2 v2)
            {
                Touching = Touch;
                Vector = v2;
            }
        }

        public CollisionResult IsTouchingLeft(Collision another, float Velocity)
        {
            Rectangle rec = GetCollisionRectangle();
            Rectangle rec2 = another.GetCollisionRectangle();

            bool Touching = rec.Right + Velocity > rec2.Left && rec.Left < rec2.Left 
            && rec.Bottom > rec2.Top && rec.Top < rec2.Bottom;

            Vector2 v2 = new Vector2(Velocity, 0);

            if (Touching)
            {
                v2.X = 0;
            }

            CollisionResult Result = new CollisionResult(Touching, v2);

            return Result;
        }

        public CollisionResult IsTouchingRight(Collision another, float Velocity)
        {
            Rectangle rec = GetCollisionRectangle();
            Rectangle rec2 = another.GetCollisionRectangle();

            bool Touching = rec.Left + Velocity < rec2.Right && rec.Right > rec2.Right 
            && rec.Bottom > rec2.Top && rec.Top < rec2.Bottom;

            Vector2 v2 = new Vector2(Velocity, 0);

            if (Touching)
            {
                v2.X = 0;
            }

            CollisionResult Result = new CollisionResult(Touching, v2);

            return Result;
        }

        public CollisionResult IsTouchingTop(Collision another, float Velocity)
        {
            Rectangle rec = GetCollisionRectangle();
            Rectangle rec2 = another.GetCollisionRectangle();

            bool Touching = rec.Bottom + Velocity > rec2.Top && rec.Top < rec2.Top 
            && rec.Right > rec2.Left && rec.Left < rec2.Right;

            Vector2 v2 = new Vector2(0, Velocity);

            if (Touching)
            {
                v2.Y = 0;
            }

            CollisionResult Result = new CollisionResult(Touching, v2);

            return Result;
        }

        public CollisionResult IsTouchingBottom(Collision another, float Velocity)
        {
            Rectangle rec = GetCollisionRectangle();
            Rectangle rec2 = another.GetCollisionRectangle();

            bool Touching = rec.Top + Velocity < rec2.Bottom && rec.Bottom > rec2.Bottom 
            && rec.Right > rec2.Left && rec.Left < rec2.Right;

            Vector2 v2 = new Vector2(0, Velocity);

            if (Touching)
            {
                v2.Y = 0;
            }


            CollisionResult Result = new CollisionResult(Touching, v2);

            return Result;
        }
    }
}
