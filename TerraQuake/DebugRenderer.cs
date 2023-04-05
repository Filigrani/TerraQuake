using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalloonInvasion
{
    internal class DebugRenderer : Component
    {
        public Layer Layer;
        public Rectangle RenderBounds;
        public Vector2 RenderOffset = Vector2.Zero;
        public Collision Colider;
        public Texture2D Sprite;
        public DebugRenderer(Collision Col)
        {
            Layer = LayersManager.GetLayer("Debug");
            Colider = Col;
            RenderBounds = new Rectangle(0, 0, Col.Widgth, Col.Height);
            Sprite = ContentManager.GetSprite("GreenTransperent");
            Layer.AddRenderer(this);
        }
        public DebugRenderer(string _Layer, Rectangle _RenderBounds)
        {
            Layer = LayersManager.GetLayer(_Layer);
            RenderBounds = _RenderBounds;
            Sprite = ContentManager.GetSprite("GreenTransperent");
            Layer.AddRenderer(this);
        }
        public void Render()
        {
            Vector2 PositionOnScreen = Layer.Position + Object.Position * LayersManager.Scaler;
            PositionOnScreen += RenderOffset* LayersManager.Scaler;

            Rectangle RenderRect = new Rectangle((int)PositionOnScreen.X, (int)PositionOnScreen.Y, RenderBounds.Width, RenderBounds.Height);
            Rectangle ScreenRect = new Rectangle(0, 0, ContentManager.Game.WindowWidth, ContentManager.Game.WindowHeight);

            if (RenderRect.Intersects(ScreenRect))
            {
                ContentManager.SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, null);
                ContentManager.SpriteBatch.Draw(Sprite, PositionOnScreen, RenderBounds, Color.White, 0, Vector2.Zero, LayersManager.Scaler, SpriteEffects.None, Layer.Depth);

                ContentManager.SpriteBatch.End();
            }
        }
        public override void OnDestroy()
        {
            Layer.RemoveRenderer(this);
        }
    }
}
