using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalloonInvasion
{
    internal class ProgressBar : Component
    {
        public Renderer BgRenderer = null;
        public Renderer FillRenderer = null;
        public float Max = 100;
        public float Val = 0;
        public Rectangle BarSize = Rectangle.Empty;
        public Vector2 Offset = Vector2.Zero;
        public bool Visible = true;

        public override void Update(GameTime gameTime)
        {
            
            if(BgRenderer != null && FillRenderer != null)
            {
                BgRenderer.RenderBounds = BarSize;
                BgRenderer.RenderOffset = Offset;
                BgRenderer.Visible = Visible;
                FillRenderer.RenderBounds.X = BarSize.X;
                FillRenderer.RenderBounds.Y = BarSize.Y;
                FillRenderer.RenderBounds.Height = BarSize.Height;

                float Fill = Val / Max;
                float W = Fill * BarSize.Width;


                FillRenderer.RenderBounds.Width = (int)W;

                if(Val < Max)
                {
                    BgRenderer.RenderOffset = Offset;
                }

                FillRenderer.RenderOffset = Offset;
                FillRenderer.Visible = Visible;
            }
        }
    }
}
