using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace TerraQuake
{
    internal class PlayerController : Component
    {
        public Collision MyCollision = null;
        public Renderer MyRenderer = null;
        public float Speed = 3;
        public bool CanJump = true;
        public bool Falling = false;
        public bool Flying = false;
        public float JumpVelocity = 0;
        public float MaxJumpVelocity = 20;
        public float Gravity = 6f;
        public Vector2 CurrentVelocity = new();

        public override void Update(GameTime gameTime)
        {
            Vector2 tempVelocity = new Vector2();

            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                tempVelocity.X = Speed;
                MyRenderer.SpriteEffect = SpriteEffects.None;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                tempVelocity.X+= -Speed;
                MyRenderer.SpriteEffect = SpriteEffects.FlipHorizontally;
            }

            if (JumpVelocity <= 0)
            {
                tempVelocity.Y = Gravity;
            }else{
                tempVelocity.Y = -JumpVelocity;
                JumpVelocity -= 1.3f;
            }
            
            tempVelocity = Collision.ValidateMovement(Object, tempVelocity);
            Move(tempVelocity);

            Falling = tempVelocity.Y > 0;
            Flying = tempVelocity.Y < 0;


            if(!Flying && JumpVelocity > 0)
            {
                JumpVelocity = 0;
                tempVelocity.Y = Gravity;
                Falling = true;
            }

            if (!CanJump && !Falling && !Flying && JumpVelocity <= 0)
            {
                CanJump = true;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                if (CanJump && !Falling && !Flying)
                {
                    CanJump = false;
                    Jump();
                }
            }

            CurrentVelocity = tempVelocity;

            LayersManager.ScrollTo(Object.Position);
            GameObjectManager.PerspectivePosition = Object.Position;
        }

        public void Jump()
        {
            JumpVelocity = MaxJumpVelocity;
        }

        public void Move(Vector2 Velocity)
        {
            Object.Position.X += Velocity.X;
            Object.Position.Y += Velocity.Y;
        }
    }
}
