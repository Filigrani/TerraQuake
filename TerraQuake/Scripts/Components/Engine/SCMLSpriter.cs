using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SpriterDotNet;
using SpriterDotNet.MonoGame;
using SpriterDotNet.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class SCMLSpriter : Component
    {
        public MonoGameAnimator Animator = null;
        public Spriter Spriter = null;
        public string SCMLName = "";
        public List<Renderer> Renderers = new List<Renderer>();
        public override void OnAttached()
        {
            for (int i = 0; i < 32; i++)
            {
                Renderer Render = new Renderer("Main");
                Object.AddComponent(Render, "Rend_" + i);
                Renderers.Add(Render);
            }
        }
        public SCMLSpriter(string Name)
        {
            SCMLName = Name;
            string SCMLData = ContentManager.GetSCMLData(SCMLName);
            if (string.IsNullOrEmpty(SCMLData))
            {
                return;
            }
            Spriter = SpriterReader.Default.Read(SCMLData);
            Animator = new MonoGameAnimator(Spriter.Entities.First());
        }
        
        public override void Update(GameTime gameTime)
        {
            if (Animator == null)
            {
                return;
            }
            Animator.Update(gameTime.ElapsedGameTime.Milliseconds);
            float X = Animator.FrameData.SpriteData[1].X;
            float Y = Animator.FrameData.SpriteData[1].Y;
        }

        public override void Render(GameTime gameTime)
        {
            if (Animator == null || Animator.FrameData == null || Renderers.Count == 0)
            {
                return;
            }
            Animator.Scale = new Vector2(0.5f, 0.5f);
            int i = 0;
            foreach (SpriterObject obj in Animator.FrameData.SpriteData)
            {
                string SpriteName = SCMLName + @"\" + Spriter.Folders[obj.FolderId].Files[obj.FileId].Name;
                Texture2D Sprite = ContentManager.GetSprite(SpriteName.Replace(".png",""), true);
                Renderer Rend = Renderers[i];
                Rend.SetSprite(Sprite);
                Vector2 Pos = Animator.GetPosition(obj);
                Vector2 Pivot = new Vector2(obj.PivotX, (1 - obj.PivotY));
                float Xoffset = Sprite.Width * Pivot.X;
                float Yoffset = Sprite.Height * Pivot.Y;
                Rend.RenderOffset = new Vector2(Xoffset-Pos.X, -Yoffset-Pos.Y);
                Rend.Rotation = MathHelper.ToRadians(-obj.Angle);
                Rend.RenderOrigin = Pivot;
                Rend.Scale = new Vector2(obj.ScaleX, obj.ScaleY) * Animator.Scale;
                i++;
            }
        }
    }
}
