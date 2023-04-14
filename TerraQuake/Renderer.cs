using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class Renderer : Component
    {
        public bool Visible = true;
        public Texture2D Sprite;
        public Rectangle RenderBounds = Rectangle.Empty;
        public Vector2 RenderOffset = Vector2.Zero;
        public Vector2 RenderOrigin = Vector2.Zero;
        public int FrameOffset = 0;
        public int Frame = 0;

        public Color Tint = Color.White;
        public Layer Layer = null;
        public SpriteEffects SpriteEffect = SpriteEffects.None;
        public float Depth = -1;
        public float Rotation = 0;
        public float Scale = 1;

        public Renderer(string _Layer)
        {
            Layer = LayersManager.GetLayer(_Layer);
            Layer.AddRenderer(this);
        }

        public void SetSprite(Texture2D Tex)
        {
            Sprite = Tex;
            RenderBounds = Tex.Bounds;
        }
        public void SetSprite(Texture2D Tex, Rectangle Bounds)
        {
            Sprite = Tex;
            RenderBounds = Bounds;
        }
        public void SetSprite(Color[] Color, int W, int H)
        {
            if(Sprite == null)
            {
                Sprite = new Texture2D(ContentManager.Game.GraphicsDevice, W, H);
                RenderBounds = Sprite.Bounds;
            }
            Sprite.SetData(Color);
        }
        public void ChangeBounds(Rectangle Bounds)
        {
            RenderBounds = Bounds;
        }

        public void Render()
        {
            
            if (!Visible || Object == null || Sprite == null)
            {
                return;
            }

            Vector2 PositionOnScreen = Layer.Position + Object.Position * LayersManager.Scaler;
            PositionOnScreen += RenderOffset*LayersManager.Scaler;
            Rectangle TempBound = RenderBounds;

            if (FrameOffset != 0)
            {
                TempBound.Width = FrameOffset;
                TempBound.X = FrameOffset * Frame;
            }

            if(Depth == -1)
            {
                Depth = Layer.Depth;
            }
            
            Rectangle RenderRect = new Rectangle((int)PositionOnScreen.X, (int)PositionOnScreen.Y, TempBound.Width, TempBound.Height);
            
            Rectangle ScreenRect = new Rectangle(0, 0, ContentManager.Game.WindowWidth, ContentManager.Game.WindowHeight);

            //if (RenderRect.Intersects(ScreenRect))
            //{
                ContentManager.SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null,null,null,null);
                ContentManager.SpriteBatch.Draw(Sprite, PositionOnScreen, TempBound, Tint, Rotation, RenderOrigin, LayersManager.Scaler * Scale, SpriteEffect, Depth);
                ContentManager.SpriteBatch.End();
            //}
        }
        public override void OnDestroy()
        {
            Layer.RemoveRenderer(this);
        }
    }
}
