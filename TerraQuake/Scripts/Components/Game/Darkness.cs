using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static TerraQuake.Terrain;

namespace TerraQuake
{
    internal class Darkness : Component
    {
        public Renderer MyRenderer = null;
        public Color[] ColorData = null;
        public Texture2D Texture = null;
        public int DisableHeight = 40;
        public List<LightSource> Lights = new List<LightSource>();

        public class LightSource
        {
            public Point Position = new Point(0,0);
            public int Radius = 0;
        }

        public int GetIndex(int x, int y)
        {
            return y * Texture.Width + x;
        }

        public void MakeHole(int X, int Y, int Radius, float MinimalAlpha)
        {
            int StartX = X - Radius;
            int EndX = X + Radius;
            int StartY = Y - Radius;
            int EndY = Y + Radius;

            if (StartX < 0)
            {
                StartX = 0;
            } else if (StartX > Texture.Width)
            {
                StartX = Texture.Width;
            }
            if (EndX < 0)
            {
                EndX = 0;
            } else if (EndX > Texture.Width)
            {
                EndX = Texture.Width;
            }
            if (StartY < 0)
            {
                StartY = 0;
            } else if (StartY > Texture.Height)
            {
                StartY = Texture.Height;
            }
            if (EndY < 0)
            {
                EndY = 0;
            } else if (EndY > Texture.Height)
            {
                EndY = Texture.Height;
            }
            for (int iY = StartY; iY != EndY; iY++)
            {
                for (int iX = StartX; iX != EndX; iX++)
                {
                    int num = iX - X;
                    int num2 = iY - Y;
                    float Dis = MathF.Sqrt(num * num + num2 * num2);
                    if (Dis <= Radius)
                    {
                        float Alpha = ProcentFromDistance(Dis, Radius) / 100;

                        if(Alpha > MinimalAlpha)
                        {
                            Alpha = MinimalAlpha;
                        }


                        Color c = new Color(0, 0, 0, Alpha);
                        ColorData[GetIndex(iX, iY)] = c;
                    }
                }
            }
        }

        public void ApplyLights(float Alpha)
        {
            GameInstance G = ContentManager.Game;
            foreach (LightSource Light in Lights.ToList())
            {
                MakeHole(Light.Position.X, Light.Position.Y, Light.Radius, Alpha);
            }

            if(G.MyGhost != null)
            {
                Point P = G.MyGhost.Object.Position.ToPoint();
                MakeHole(P.X, P.Y, 120, Alpha);
            }
        }


        public void CreateDarkness(Color FillColor, float Alpha)
        {
            GameInstance G = ContentManager.Game;
            Terrain Terra = G.TerrainInstance;

            if(Texture == null)
            {
                Texture = new Texture2D(G.GraphicsDevice, Terra.TerrainW, Terra.TerrainH);
                MyRenderer.SetSprite(Texture);
            }
            if(ColorData == null)
            {
                ColorData = new Color[Terra.TerrainW * Terra.TerrainH];
            }
            for (int i = 0; i < ColorData.Length; i++)
            {
                ColorData[i] = FillColor;
            }
            ApplyLights(Alpha);
            Texture.SetData(ColorData);
        }

        public float ProcentFromDistance(float Distance, float MaxDistance)
        {
            return ((100f * Distance) / MaxDistance);
        }

        public override void Render(GameTime gameTime)
        {
            GameInstance G = ContentManager.Game;
            if (G.MyGhost != null)
            {
                if(G.MyGhost.Object.Position.Y < DisableHeight)
                {
                    MyRenderer.Visible = false;
                } else
                {
                    MyRenderer.Visible = true;
                    float num = 0;
                    float num2 = G.MyGhost.Object.Position.Y - DisableHeight;
                    float Dis = MathF.Sqrt(num * num + num2 * num2);
                    float Alpha = 0.1f + ProcentFromDistance(Dis, DisableHeight*8) / 100;
                    Color c = new Color(0, 0, 0, Alpha);
                    CreateDarkness(c, Alpha);
                }
            }
        }
    }
}
