using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class MeltRenderer : Component
    {
        public Renderer MyRenderer = null;
        public List<Slice> Slices = new List<Slice>();
        public Color[] ColorData = null;
        public bool ColorDataModified = false;
        public int SliceSize = 4;
        public int Push = 8;
        public int ProcessesRequired = 0;
        public int ImagePushRequired = 0;
        public Task UpdateThread = null;
        public bool UpdateThreadCreated = false;

        public class Slice
        {
            public int Offset = 0;
            public int X = 0;

            public Slice(int offset, int x)
            {
                Offset = offset;
                X = x;
            }
        }

        public MeltRenderer(Renderer renderer)
        {
            MyRenderer = renderer;
        }

        public int GetIndex(int x, int y)
        {
            if(MyRenderer == null)
            {
                return 0;
            }
            return y * MyRenderer.Sprite.Width + x;
        }

        public void StartMelt()
        {
            if(MyRenderer == null)
            {
                return;
            }
            Random RNG = new Random();
            int Offset = RNG.Next(0, 15);
            int LongestOffset = -255;
            for (int iX = 0; iX < MyRenderer.Sprite.Width; iX += SliceSize)
            {
                Slices.Add(new Slice(Offset, iX));
                Offset = Offset + RNG.Next(-1, 2);

                if(LongestOffset < Offset)
                {
                    LongestOffset = Offset;
                }
            }

            ProcessesRequired = LongestOffset;
            ImagePushRequired = MyRenderer.Sprite.Height / Push;
        }

        public void Melting()
        {
            for (int i = 0; i < Slices.Count; i++)
            {
                Slice S = Slices[i];
                if (S.Offset > 0)
                {
                    S.Offset--;
                } else
                {
                    for (int j = 1; j <= Push; j++)
                    {
                        for (int iX = S.X; iX <= S.X + SliceSize - 1; iX++)
                        {
                            for (int iY = MyRenderer.Sprite.Height - 1; iY != -1; iY--)
                            {
                                int CurVal = GetIndex(iX, iY);
                                int BottomVal = GetIndex(iX, iY + 1);

                                if (iY < MyRenderer.Sprite.Height - 1)
                                {
                                    Color C = ColorData[CurVal];
                                    ColorData[BottomVal] = C;
                                    ColorData[CurVal] = Color.Transparent;
                                } else
                                {
                                    ColorData[CurVal] = Color.Transparent;
                                }
                            }
                        }
                    }
                    ColorDataModified = true;
                }
            }
            ProcessesRequired--;
        }


        public override void Update(GameTime gameTime)
        {
            if (MyRenderer != null)
            {
                if(ColorData == null)
                {
                    ColorData = new Color[MyRenderer.Sprite.Width * MyRenderer.Sprite.Height];
                    MyRenderer.Sprite.GetData(ColorData);
                }

                if(ProcessesRequired > 0)
                {
                    if (!UpdateThreadCreated || (UpdateThreadCreated && (UpdateThread == null || UpdateThread.IsCompleted)))
                    {
                        if (UpdateThread != null)
                        {
                            UpdateThread.Dispose();
                            UpdateThread = null;
                        }
                        UpdateThreadCreated = true;
                        UpdateThread = Task.Factory.StartNew(Melting);
                    }
                } else
                {
                    if(ImagePushRequired > 0)
                    {
                        Object.Position.Y += 8;
                        ImagePushRequired--;
                    } else
                    {
                        GameObjectManager.DestroyObject(Object.GUID);
                    }
                }
            }
        }

        public override void Render(GameTime gameTime)
        {
            if (ColorDataModified)
            {
                MyRenderer.Sprite.SetData(ColorData);
                ColorDataModified = false;
            }
        }
    }
}
