using BalloonInvasion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;

namespace TerraQuake
{
    internal class Terrain
    {
        public int TerrainW = 2050;
        public int TerrainH = 870;

        public TerrainPixel[,] Pixels;
        public Color[] TerrainColorData;
        public Renderer Renderer = null;
        public Renderer RendererBackground = null;
        public GameObject TerrainObject = null;
        public List<Point> PixelsChanged = new List<Point>();
        public List<ProcessPixelElement> ProcessQueue = new List<ProcessPixelElement>();
        public bool NoRenderUpdate = false;
        public bool ReadyForRender = false;
        public bool ManualUpdate = false;
        public bool NoRolling = true;
        public bool NoWaterFlow = false;
        public bool NoAbovePing = false;

        public int LongetsUpdateMs = 0;
        public int LongetsRenderMs = 0;
        public Terrain()
        {
            TerrainObject = GameObjectManager.CreateObject();
            Renderer = new Renderer("Objects");
            RendererBackground = new Renderer("BG");
            RendererBackground.Tint = new Color(167, 167, 167);
            TerrainObject.AddComponent(Renderer, "Main");
            TerrainObject.AddComponent(RendererBackground, "BG");
            TerrainObject.Position = new Vector2(0, 0);
        }

        public class TerrainPixel
        {
            public Color Color = Color.Transparent;
            public int Rolling = 0;
            public int RollingDir = 0;
            public bool Fallable = true;
            public bool Water = false;
            public bool HasBackground = false;
            public bool SkipProcess = false;

            public void Delete()
            {
                Color = Color.Transparent;
                Rolling = 0;
                RollingDir = 0;
                Fallable = true;
                Water = false;
            }

            public void MoveTo(TerrainPixel New)
            {
                New.Color = Color;
                New.Rolling = Rolling;
                New.RollingDir = RollingDir;
                New.Fallable = Fallable;
                New.Water = Water;
            }

            public bool IsAir()
            {
                return Color == Color.Transparent;
            }
            public bool IsWater()
            {
                return Water;
            }

            public bool IsFallable()
            {
                if (IsAir())
                {
                    return false;
                }
                if (IsWater())
                {
                    return true;
                }
                return Fallable;
            }
        }

        public class ProcessPixelElement
        {
            public int X = 0;
            public int Y = 0;
            public int Priority = 0;

            public ProcessPixelElement(int x, int y, int height)
            {
                X = x; 
                Y = y;
                Priority = (height - y + 1) * x;
            }
        }

        public void AddProcessPixel(int X, int Y)
        {
            ProcessQueue.Add(new ProcessPixelElement(X, Y, TerrainH));
        }

        public void AddChangedPixel(int X, int Y)
        {
            PixelsChanged.Add(new Point(X, Y));
        }

        readonly int[] RandomDirections = { -1, 0, 0, -1, -1, 0, 0, -1, 0, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, 0, -1, -1, 0, 0, 0, -1, 0, -1, 0, 0 };
        private int LastRandomDirectionIndex = 0;

        public int GetRandomDirection()
        {
            if (LastRandomDirectionIndex == 31)
            {
                LastRandomDirectionIndex = 0;
            }
            int Val = RandomDirections[LastRandomDirectionIndex];
            LastRandomDirectionIndex++;
            return Val;
        }
        readonly int[] RandomRollingNums = { 8, 7, 3, 3, 9, 3, 10, 5, 3, 8, 5, 6, 7, 10, 8, 10, 4, 7, 4, 10, 5, 5, 9, 10, 3, 3, 3, 9, 6, 4, 6, 8, 6};
        private int LastRandomRollingNumsIndex = 0;

        public int GetRandomRollingNums()
        {
            if (NoRolling)
            {
                return 0;
            }
            
            
            if (LastRandomRollingNumsIndex == 33)
            {
                LastRandomRollingNumsIndex = 0;
            }
            int Val = RandomRollingNums[LastRandomRollingNumsIndex];
            LastRandomRollingNumsIndex++;
            return Val;
        }

        readonly int[] RandomColors = { 3, 4, 1, 5, 1, 5, 2, 1, 1, 1, 5, 5, 2, 2, 3, 1, 5, 3, 1, 6, 1, 6, 5, 2, 2, 5, 3, 3, 6, 2, 6, 4, 4 };
        private int LastRandomColorsIndex = 0;
        public Random ShuffleRandom = new Random(778834);
        public Random WorldGenRandom = new Random(778834);

        public void Shuffle<T>(T[] array)
        {
            int n = array.Length;
            for (int i = 0; i < (n - 1); i++)
            {
                int r = i + ShuffleRandom.Next(n - i);
                T t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
        }


        public int GetRandomColor()
        {
            if (LastRandomColorsIndex == 33)
            {
                LastRandomColorsIndex = 0;
                Shuffle(RandomColors);
            }
            int Val = RandomColors[LastRandomColorsIndex];
            LastRandomColorsIndex++;
            return Val;
        }

        public void ProcessPixelNew(int iX, int iY)
        {
            TerrainPixel Px = Pixels[iX, iY];
            if (Px.Fallable)
            {
                TerrainPixel PxBelow = null;
                if(iY + 1 != TerrainH)
                {
                    PxBelow = Pixels[iX, iY + 1];
                    if (PxBelow.IsAir())
                    {
                        Px.MoveTo(PxBelow);
                        Px.Delete();
                    }
                }
            }
        }

        public void ProcessPixel(int iX, int iY)
        {
            TerrainPixel Px = Pixels[iX, iY];
            TerrainPixel PxLeft = null;
            TerrainPixel PxRight = null;
            bool UpdateAbove = false;

            if (iX - 1 != -1) // Something on the left
            {
                PxLeft = Pixels[iX - 1, iY];
            }
            if (iX + 1 != TerrainW) // Something on the right
            {
                PxRight = Pixels[iX + 1, iY];
            }

            if (!NoWaterFlow)
            {
                if (Px.IsWater())
                {
                    if (PxRight != null && PxRight.IsAir())
                    {
                        AddProcessPixel(iX, iY);
                    } else if (PxLeft != null && PxLeft.IsAir())
                    {
                        AddProcessPixel(iX, iY);
                    }
                }
            }

            if (iY + 1 != TerrainH) // This is not last layer.
            {
                TerrainPixel PxBelow = Pixels[iX, iY + 1];
                TerrainPixel PxAbove = null;
                if (iY - 1 != -1) // Something above
                {
                    PxAbove = Pixels[iX, iY - 1];
                }

                if (PxBelow.IsAir()) // Air below can fall down.
                {
                    if (Px.Rolling == 0)
                    {
                        if (!Px.IsWater())
                        {
                            Px.Rolling = GetRandomRollingNums();
                        } else
                        {
                            Px.Rolling = 0;
                        }
                    }
                    if (PxAbove != null && PxAbove.IsFallable()) // Chain reaction
                    {
                        UpdateAbove = true;
                    }
                    if (PxLeft != null && PxLeft.IsWater()) // Chain reaction of water flow
                    {
                        AddProcessPixel(iX - 1, iY);
                    }
                    if (PxRight != null && PxRight.IsWater()) // Chain reaction of water flow
                    {
                        AddProcessPixel(iX + 1, iY);
                    }
                    Px.MoveTo(PxBelow); // Move down
                    Px.Delete(); // Turn current pixel to air.
                    AddProcessPixel(iX, iY + 1);
                    // Re-render this pixels
                    AddChangedPixel(iX, iY);
                    AddChangedPixel(iX, iY + 1);
                } else if (!NoRolling && ((Px.Water && Px.Rolling > -100) || Px.Rolling > 0)) // Ability to roll left and right.
                {
                    Px.Rolling--; // Remove 1 from rolling counts.

                    bool CanRollRight = false;
                    bool CanRollLeft = false;
                    if (PxRight != null)
                    {
                        CanRollRight = PxRight.IsAir();
                    }
                    if (PxLeft != null)
                    {
                        CanRollLeft = PxLeft.IsAir();
                    }

                    if (CanRollLeft && CanRollRight)
                    {
                        Px.RollingDir = GetRandomDirection();
                    } else if (CanRollLeft)
                    {
                        Px.RollingDir = -1;
                    } else if (CanRollRight)
                    {
                        Px.RollingDir = 1;
                    } else
                    {
                        Px.Rolling = 0;
                        Px.RollingDir = 0;
                    }
                    if (PxAbove != null && PxAbove.IsFallable()) // Chain reaction
                    {
                        UpdateAbove = true;
                    }
                    int Dir = Px.RollingDir;

                    if (Dir != 0)
                    {
                        Px.MoveTo(Pixels[iX + Dir, iY]);
                        Px.Delete();
                        AddProcessPixel(iX + Dir, iY);

                        AddChangedPixel(iX + Dir, iY);
                        AddChangedPixel(iX, iY);
                    }
                }
            }
            if (!NoAbovePing && UpdateAbove)
            {
                ProcessPixel(iX, iY-1);
            }
        }

        //public void ProcessPixel(int iX, int iY)
        //{
        //    if (Pixels[iX, iY].IsFallable()) // Pixel is dynamic.
        //    {
        //        TerrainPixel Px = Pixels[iX, iY];
        //        if (iY + 1 != TerrainH) // This is not last layer.
        //        {
        //            TerrainPixel PxBelow = Pixels[iX, iY + 1];
        //            if (PxBelow.IsAir()) // Air below can fall down.
        //            {
        //                if (Px.Rolling == 0)
        //                {
        //                    if (!Px.IsWater())
        //                    {
        //                        Px.Rolling = GetRandomRollingNums();
        //                    } else
        //                    {
        //                        Px.Rolling = 0;
        //                    }
        //                }
        //                Px.MoveTo(PxBelow); // Move down
        //                Px.Delete(); // Turn current pixel to air.

        //                // Re-render this pixels
        //                AddChangedPixel(iX, iY); 
        //                AddChangedPixel(iX, iY + 1);
        //            } else if ((Px.Water && Px.Rolling > -100) || Px.Rolling > 0) // Ability to roll left and right.
        //            {
        //                Px.Rolling--; // Remove 1 from rolling counts.

        //                bool CanRollRight = false;
        //                bool CanRollLeft = false;
        //                if (iX + 1 < TerrainW)
        //                {
        //                    CanRollRight = Pixels[iX + 1, iY].IsAir();
        //                }
        //                if (iX - 1 >= 0)
        //                {
        //                    CanRollLeft = Pixels[iX - 1, iY].IsAir();
        //                }

        //                if (CanRollLeft && CanRollRight)
        //                {
        //                    Px.RollingDir = GetRandomDirection();
        //                } else if (CanRollLeft)
        //                {
        //                    Px.RollingDir = -1;
        //                } else if (CanRollRight)
        //                {
        //                    Px.RollingDir = 1;
        //                } else
        //                {
        //                    Px.Rolling = 0;
        //                    Px.RollingDir = 0;
        //                }
        //                int Dir = Px.RollingDir;

        //                if (Dir != 0)
        //                {
        //                    Px.MoveTo(Pixels[iX + Dir, iY]);
        //                    Px.Delete();
        //                    AddChangedPixel(iX + Dir, iY);
        //                    AddChangedPixel(iX, iY);
        //                }
        //            }
        //        }
        //    }
        //}
        //public void ProcessPixel(int iX, int iY)
        //{
        //    if (Pixels[iX, iY].IsFallable())
        //    {
        //        if (iY + 1 < TerrainH) // This is not last layer
        //        {
        //            if (Pixels[iX, iY + 1].IsAir()) // Air bellow, falling down.
        //            {
        //                if (Pixels[iX, iY].Rolling == 0)
        //                {
        //                    if(!Pixels[iX, iY].IsWater())
        //                    {
        //                        Pixels[iX, iY].Rolling = GetRandomRollingNums();
        //                    } else
        //                    {
        //                        Pixels[iX, iY].Rolling = 0;
        //                    }
        //                }
        //                Pixels[iX, iY + 1].Color = Pixels[iX, iY].Color;
        //                Pixels[iX, iY + 1].Rolling = Pixels[iX, iY].Rolling;
        //                Pixels[iX, iY + 1].RollingDir = Pixels[iX, iY].RollingDir;
        //                Pixels[iX, iY + 1].Water = Pixels[iX, iY].Water;
        //                Pixels[iX, iY + 1].Fallable = Pixels[iX, iY].Fallable;
        //                Pixels[iX, iY].Delete();
        //                AddChangedPixel(iX, iY);
        //                AddChangedPixel(iX, iY+1);
        //            } else if ((Pixels[iX, iY].Water && Pixels[iX, iY].Rolling > -100) || Pixels[iX, iY].Rolling > 0)
        //            {
        //                Pixels[iX, iY].Rolling--;

        //                bool CanRollRight = false;
        //                bool CanRollLeft = false;
        //                if (iX + 1 < TerrainW)
        //                {
        //                    CanRollRight = Pixels[iX + 1, iY].IsAir();
        //                }
        //                if (iX - 1 >= 0)
        //                {
        //                    CanRollLeft = Pixels[iX - 1, iY].IsAir();
        //                }

        //                if (CanRollLeft && CanRollRight)
        //                {
        //                    Pixels[iX, iY].RollingDir = GetRandomDirection();
        //                } else if (CanRollLeft)
        //                {
        //                    Pixels[iX, iY].RollingDir = -1;
        //                } else if (CanRollRight)
        //                {
        //                    Pixels[iX, iY].RollingDir = 1;
        //                } else
        //                {
        //                    Pixels[iX, iY].Rolling = 0;
        //                    Pixels[iX, iY].RollingDir = 0;
        //                }
        //                int Dir = Pixels[iX, iY].RollingDir;

        //                if (Dir != 0)
        //                {
        //                    Pixels[iX + Dir, iY].Color = Pixels[iX, iY].Color;
        //                    Pixels[iX + Dir, iY].Rolling = Pixels[iX, iY].Rolling;
        //                    Pixels[iX + Dir, iY].RollingDir = Pixels[iX, iY].RollingDir;
        //                    Pixels[iX + Dir, iY].Water = Pixels[iX, iY].Water;
        //                    Pixels[iX + Dir, iY].Fallable = Pixels[iX, iY].Fallable;
        //                    Pixels[iX, iY].Delete();
        //                    AddChangedPixel(iX + Dir, iY);
        //                    AddChangedPixel(iX, iY);
        //                }
        //            }
        //        }
        //    }
        //}

        public bool ScanDirectionRight = false;

        public void FallingPixels()
        {
            if (ProcessQueue.Count > 0)
            {
                ProcessQueue.Sort(delegate (ProcessPixelElement p1, ProcessPixelElement p2) { return p1.Priority.CompareTo(p2.Priority); });
                ProcessQueue.Reverse();
                ProcessPixelElement[] SortedQueue = ProcessQueue.ToArray();
                ProcessQueue.Clear();

                if (ScanDirectionRight)
                {
                    for (int iX = 0; iX != SortedQueue.Length; iX++)
                    {
                        ProcessPixelElement cur = SortedQueue[iX];
                        ProcessPixel(cur.X, cur.Y);
                    }
                } else
                {
                    for (int iX = SortedQueue.Length - 1; iX != -1; iX--)
                    {
                        ProcessPixelElement cur = SortedQueue[iX];
                        ProcessPixel(cur.X, cur.Y);
                    }
                }
                ScanDirectionRight = !ScanDirectionRight;
            }
        }

        //public void FallingPixels()
        //{
        //    for (int iY = TerrainH - 1; iY != -1; iY--)
        //    {
        //        if (ScanDirectionRight)
        //        {
        //            for (int iX = 0; iX != TerrainW; iX++)
        //            {
        //                ProcessPixel(iX, iY);
        //            }
        //        } else
        //        {
        //            for (int iX = TerrainW - 1; iX != 1; iX--)
        //            {
        //                ProcessPixel(iX, iY);
        //            }
        //        }
        //    }
        //    ScanDirectionRight = !ScanDirectionRight;
        //}

        public Color GetGroundColor()
        {
            int Variant = GetRandomColor();
            if (Variant == 1 || Variant == 5)
            {
                return Color.Brown;
            } else if (Variant == 2 || Variant == 6)
            {
                return Color.RosyBrown;
            } else if (Variant == 3)
            {
                return Color.SaddleBrown;
            } else if (Variant == 4)
            {
                return Color.SandyBrown;
            }
            return Color.Brown;
        }
        public Color GetGrassColor()
        {
            int Variant = GetRandomColor();
            if (Variant == 1 || Variant == 5)
            {
                return Color.Green;
            } else if (Variant == 2 || Variant == 6)
            {
                return Color.ForestGreen;
            } else if (Variant == 3)
            {
                return Color.MediumSeaGreen;
            } else if (Variant == 4)
            {
                return Color.SeaGreen;
            }
            return Color.SeaGreen;
        }
        public Color GetStoneColor()
        {
            int Variant = GetRandomColor();
            if (Variant == 1)
            {
                return Color.SlateGray;
            } else if (Variant == 2)
            {
                return Color.DimGray;
            } else if (Variant == 3)
            {
                return Color.LightSlateGray;
            } else if (Variant == 4)
            {
                return Color.DarkGray;
            } else if (Variant == 5)
            {
                return Color.SlateGray;
            } else if (Variant == 6)
            {
                return Color.SlateGray;
            }
            return Color.SlateGray;
        }
        public Color GetGravelColor()
        {
            int Variant = GetRandomColor();
            if (Variant == 1)
            {
                return Color.OldLace;
            } else if (Variant == 2)
            {
                return Color.MintCream;
            } else if (Variant == 3)
            {
                return Color.MistyRose;
            } else if (Variant == 4)
            {
                return Color.RosyBrown;
            } else if (Variant == 5)
            {
                return Color.Silver;
            } else if (Variant == 6)
            {
                return Color.Lavender;
            }
            return Color.SlateGray;
        }
        public Color GetWaterColor()
        {
            int Variant = GetRandomColor();
            if (Variant == 1)
            {
                return Color.MediumBlue;
            } else if (Variant == 2)
            {
                return Color.MidnightBlue;
            } else if (Variant == 3)
            {
                return Color.Navy;
            } else if (Variant == 4)
            {
                return Color.RoyalBlue;
            } else if (Variant == 5)
            {
                return Color.SlateBlue;
            } else if (Variant == 6)
            {
                return Color.SteelBlue;
            }
            return Color.Brown;
        }

        private Color[,] TextureTo2DArray(Texture2D texture)
        {
            Color[] colors1D = new Color[texture.Width * texture.Height];
            texture.GetData(colors1D);
            Color[,] colors2D = new Color[texture.Width, texture.Height];
            for (int x = 0; x < texture.Width; x++)
            {
                for (int y = 0; y < texture.Height; y++)
                {
                    colors2D[x, y] = colors1D[x + y * texture.Width];
                }
            }
            return colors2D;
        }

        public void CreateTerrainFromImage()
        {
            Color[,] TextureData = TextureTo2DArray(ContentManager.GetSprite("TerrainTest"));
            Pixels = new TerrainPixel[TerrainW, TerrainH];
            for (int iY = 0; iY < TerrainH; iY++)
            {
                for (int iX = 0; iX < TerrainW; iX++)
                {
                    Pixels[iX, iY] = new TerrainPixel();
                    Pixels[iX, iY].Color = TextureData[iX, iY];
                }
            }
            RenderTerrain(ContentManager.Game.GraphicsDevice);
        }

        public void HillsGenerator()
        {
            for (int iY = 0; iY != TerrainH; iY++)
            {
                for (int iX = 0; iX != TerrainW; iX++)
                {
                    Pixels[iX, iY] = new TerrainPixel();
                }
            }
            int Swap = 78;
            double Multi = WorldGenRandom.NextDouble() / Swap;
            
            for (int iX = 0; iX != TerrainW; iX++)
            {
                Swap--;
                double HillTop = Math.Sin(iX * Multi) * TerrainH;

                //if(Swap == 0)
                //{
                //    Swap = WorldGenRandom.Next(8, 78);
                //    Multi = WorldGenRandom.NextDouble() / Swap;
                //}

                int HillHeigh = TerrainH - Convert.ToInt32(HillTop);

                if(HillHeigh < 0)
                {
                    HillHeigh = 0;
                }
                if(HillHeigh >= TerrainH)
                {
                    HillHeigh = TerrainH-1;
                }

                Pixels[iX, HillHeigh].Color = GetGrassColor();
                int UnitsPlaced = 0;
                for (int iY = HillHeigh; iY != TerrainH; iY++)
                {
                    UnitsPlaced++;
                    if (UnitsPlaced <= 45)
                    {
                        Pixels[iX, iY].Color = GetGrassColor();
                    } else if (UnitsPlaced > 45 && UnitsPlaced < 145)
                    {
                        Pixels[iX, iY].Color = GetGroundColor();
                        if(UnitsPlaced > 60 + WorldGenRandom.Next(0, 20))
                        {
                            Pixels[iX, iY].HasBackground = true;
                        }
                    } else
                    {
                        Pixels[iX, iY].Color = GetStoneColor();
                        Pixels[iX, iY].Fallable = false;
                        Pixels[iX, iY].HasBackground = true;
                    }
                }
            }
            RenderTerrainBackground(ContentManager.Game.GraphicsDevice);
        }

        public void AdvancedGenerator()
        {
            for (int iY = 0; iY != TerrainH; iY++)
            {
                for (int iX = 0; iX != TerrainW; iX++)
                {
                    Pixels[iX, iY] = new TerrainPixel();
                }
            }
            int HillHeigh = WorldGenRandom.Next(20, TerrainH);
            bool Up = true;
            for (int iX = 0; iX != TerrainW; iX++)
            {
                if (WorldGenRandom.Next(0, 100) > 30)
                {
                    if (Up)
                    {
                        HillHeigh -= WorldGenRandom.Next(10, 80);
                    } else
                    {
                        HillHeigh += WorldGenRandom.Next(10, 80);
                    }

                    if (WorldGenRandom.Next(0, 100) > 30)
                    {
                        Up = !Up;
                    }
                }

                if (HillHeigh < 0)
                {
                    HillHeigh = 0;
                } else if (HillHeigh > TerrainH - 1)
                {
                    HillHeigh = TerrainH - 1;
                }

                Pixels[iX, HillHeigh].Color = GetGrassColor();
                int UnitsPlaced = 0;
                for (int iY = HillHeigh; iY != TerrainH; iY++)
                {
                    UnitsPlaced++;
                    if (UnitsPlaced < 40)
                    {
                        Pixels[iX, iY].Color = GetGrassColor();
                    } else if (UnitsPlaced > 40 && UnitsPlaced < 140)
                    {
                        Pixels[iX, iY].Color = GetGroundColor();
                    } else
                    {
                        Pixels[iX, iY].Color = GetStoneColor();
                    }
                }
            }
        }
        public void SolidGenerator()
        {
            for (int iY = 0; iY < TerrainH; iY++)
            {
                for (int iX = 0; iX < TerrainW; iX++)
                {
                    TerrainPixel Px = Pixels[iX, iY] = new TerrainPixel();
                    if (iY < 150)
                    {
                        Px.Color = Color.AliceBlue;
                        Px.HasBackground = true;
                    } else if (iY >= 150)
                    {
                        Px.Color = Color.AliceBlue;
                        Px.Fallable = false;
                        Px.HasBackground = true;
                    }
                }
            }

            RenderTerrainBackground(ContentManager.Game.GraphicsDevice);
        }
        public void SimpleGenerator()
        {
            for (int iY = 0; iY < TerrainH; iY++)
            {
                for (int iX = 0; iX < TerrainW; iX++)
                {
                    TerrainPixel Px = Pixels[iX, iY] = new TerrainPixel();
                    if (iY < 150)
                    {
                        if (iY <= 40 + WorldGenRandom.Next(0, 20))
                        {
                            Px.Color = GetGrassColor();
                        } else
                        {
                            Px.Color = GetGroundColor();
                            if(iY > 60 + WorldGenRandom.Next(0, 20))
                            {
                                Px.HasBackground = true;
                            }
                        }
                    } else if (iY >= 150 + WorldGenRandom.Next(0, 25))
                    {
                        Px.Color = GetStoneColor();
                        Px.Fallable = false;
                        Px.HasBackground = true;
                    } else
                    {
                        Px.Color = GetGroundColor();
                        Px.HasBackground = true;
                    }
                }
            }

            RenderTerrainBackground(ContentManager.Game.GraphicsDevice);

            for (int i = 1; i != 20; i++)
            {
                AddPond(WorldGenRandom);
            }
        }

        public void CreateTerrain(int Seed = 0)
        {
            ReadyForRender = false;
            if (Seed == 0)
            {
                Seed = Guid.NewGuid().GetHashCode();
            }
            ShuffleRandom = new Random(Seed);
            WorldGenRandom = new Random(Seed);
            Pixels = new TerrainPixel[TerrainW, TerrainH];

            SimpleGenerator();
            //AdvancedGenerator();
            //HillsGenerator();
            //SolidGenerator();
            ReadyForRender = true;
            RenderTerrain(ContentManager.Game.GraphicsDevice);
        }

        public void BenchHoles()
        {
            MakeHole(329, 57, 20);
            MakeHole(285, 169, 20);
            MakeHole(722, 100, 20);
            MakeHole(435, 169, 20);
            MakeHole(537, 100, 70);
            MakeHole(72, 230, 70);
            MakeHole(791, 188, 70);
        }

        public Point LastHole = new Point(0,0);
        public void MakeHole(int X, int Y, int Radius)
        {
            LastHole.X = X;
            LastHole.Y = Y;
            int StartX = X - Radius;
            int EndX = X + Radius;
            int StartY = Y - Radius;
            int EndY = Y + Radius;

            if (StartX < 0)
            {
                StartX = 0;
            } else if (StartX > TerrainW)
            {
                StartX = TerrainW;
            }
            if (EndX < 0)
            {
                EndX = 0;
            } else if (EndX > TerrainW)
            {
                EndX = TerrainW;
            }
            if (StartY < 0)
            {
                StartY = 0;
            } else if (StartY > TerrainH)
            {
                StartY = TerrainH;
            }
            if (EndY < 0)
            {
                EndY = 0;
            } else if (EndY > TerrainH)
            {
                EndY = TerrainH;
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
                        Pixels[iX, iY].Delete();
                        AddChangedPixel(iX, iY);

                        if(iY -1 != -1) // Something can be above.
                        {
                            int num3 = iX - X;
                            int num4 = (iY-1) - Y;
                            float DisAbovPx = MathF.Sqrt(num3 * num3 + num4 * num4);

                            if(DisAbovPx > Radius) // Distance check failed, this is edge pixel!
                            {
                                if(Pixels[iX, iY - 1].IsFallable())
                                {
                                    AddProcessPixel(iX, iY - 1);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void MakeWater(int X, int Y)
        {
            if (X < 0 || X > TerrainW-1 || Y > TerrainH-1 || Y < 0)
            {
                return;
            }
            Pixels[X, Y].Fallable = true;
            Pixels[X, Y].Water = true;
            Pixels[X, Y].Color = GetWaterColor();
        }

        public void MakeDirt(int X, int Y, int Radius)
        {
            int StartX = X - Radius;
            int EndX = X + Radius;
            int StartY = Y - Radius;
            int EndY = Y + Radius;

            if (StartX < 0)
            {
                StartX = 0;
            } else if (StartX > TerrainW)
            {
                StartX = TerrainW;
            }
            if (EndX < 0)
            {
                EndX = 0;
            } else if (EndX > TerrainW)
            {
                EndX = TerrainW;
            }
            if (StartY < 0)
            {
                StartY = 0;
            } else if (StartY > TerrainH)
            {
                StartY = TerrainH;
            }
            if (EndY < 0)
            {
                EndY = 0;
            } else if (EndY > TerrainH)
            {
                EndY = TerrainH;
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
                        Pixels[iX, iY].Fallable = true;
                        Pixels[iX, iY].Color = GetGravelColor();
                        AddChangedPixel(iX, iY);
                    }
                }
            }
        }

        public void AddPond(Random RNG)
        {
            int PondCenterX = RNG.Next(5, TerrainW);
            int PondCenterY = RNG.Next(5, TerrainH);
            int Childs = 21;
            int MaxDistance = 20;
            for (int i = 1; i != Childs; i++)
            {
                int ChildX = PondCenterX - RNG.Next(-MaxDistance, MaxDistance);
                int ChildY = PondCenterY - RNG.Next(-MaxDistance, MaxDistance);

                int Radius = RNG.Next(5, 17);
                int StartX = ChildX - Radius;
                int EndX = ChildX + Radius;
                int StartY = ChildY - Radius;
                int EndY = ChildY + Radius;

                if (StartX < 0)
                {
                    StartX = 0;
                } else if (StartX > TerrainW)
                {
                    StartX = TerrainW;
                }
                if (EndX < 0)
                {
                    EndX = 0;
                } else if (EndX > TerrainW)
                {
                    EndX = TerrainW;
                }
                if (StartY < 0)
                {
                    StartY = 0;
                } else if (StartY > TerrainH)
                {
                    StartY = TerrainH;
                }
                if (EndY < 0)
                {
                    EndY = 0;
                } else if (EndY > TerrainH)
                {
                    EndY = TerrainH;
                }

                for (int iY = StartY; iY != EndY; iY++)
                {
                    for (int iX = StartX; iX != EndX; iX++)
                    {
                        int num = iX - ChildX;
                        int num2 = iY - ChildY;
                        float Dis = MathF.Sqrt(num * num + num2 * num2);
                        if (Dis <= Radius)
                        {
                            Pixels[iX, iY].Color = GetWaterColor();
                            Pixels[iX, iY].Water = true;
                        }
                    }
                }
            }
        }

        public void RenderTerrainChanges(GraphicsDevice GDevice)
        {
            if (TerrainColorData == null)
            {
                TerrainColorData = new Color[TerrainW * TerrainH];
            }
            foreach (Point Change in PixelsChanged)
            {
                int Index = Change.X + TerrainW * Change.Y;
                TerrainColorData[Index] = Pixels[Change.X, Change.Y].Color;
            }
            Renderer.Sprite.SetData(TerrainColorData);
            PixelsChanged.Clear();
        }

        public void RenderTerrainBackground(GraphicsDevice GDevice)
        {
            Color[] BackgroundColorData = new Color[TerrainW * TerrainH];
            int ColorX = 0;
            int ColorY = 0;
            int Index = 0;
            while (Index < BackgroundColorData.Length)
            {
                if (ColorX == TerrainW)
                {
                    ColorX = 0;
                    ColorY++;
                }
                if (Pixels[ColorX, ColorY].HasBackground)
                {
                    BackgroundColorData[Index] = Pixels[ColorX, ColorY].Color;
                }
                Index++;
                ColorX++;
            }
            RendererBackground.SetSprite(BackgroundColorData, TerrainW, TerrainH);
        }

        public void RenderTerrain(GraphicsDevice GDevice)
        {
            if (TerrainColorData == null)
            {
                TerrainColorData = new Color[TerrainW * TerrainH];
            }
            int ColorX = 0;
            int ColorY = 0;
            int Index = 0;
            while (Index < TerrainColorData.Length)
            {
                if (ColorX == TerrainW)
                {
                    ColorX = 0;
                    ColorY++;
                }
                Color PixelColor = Pixels[ColorX, ColorY].Color;
                if(PixelColor != Color.Transparent)
                {
                    TerrainColorData[Index] = PixelColor;
                }
                Index++;
                ColorX++;
            }
            Renderer.SetSprite(TerrainColorData, TerrainW, TerrainH);
        }

        public int LastScanX = 0;
        public int LastScanXEnd = 0;

        public int LastScanY = 0;
        public int LastScanYEnd = 0;
        public bool CheckCollision(Rectangle Rect)
        {
            bool OutOfTerrain = false;
            if(Rect.Bottom < 0)
            {
                OutOfTerrain = true;
            }
            if (Rect.Left > TerrainW)
            {
                OutOfTerrain = true;
            }
            if (Rect.Right < 0)
            {
                OutOfTerrain = true;
            }

            int StartX = Rect.X;
            int EndX = Rect.X+Rect.Width;
            int StartY = Rect.Y;
            int EndY = Rect.Y + Rect.Height;

            if(StartX < 0)
            {
                StartX = 0;
            }else if(StartX > TerrainW)
            {
                StartX = TerrainW;
            }
            if (EndX < 0)
            {
                EndX = 0;
            } else if (EndX > TerrainW)
            {
                EndX = TerrainW;
            }
            if (StartY < 0)
            {
                StartY = 0;
            } else if (StartY > TerrainH)
            {
                StartY = TerrainH;
            }
            if (EndY < 0)
            {
                EndY = 0;
            } else if (EndY > TerrainH)
            {
                EndY = TerrainH;
            }

            LastScanX = StartX;
            LastScanXEnd = EndX;

            LastScanY = StartY;
            LastScanYEnd = EndY;

            if (OutOfTerrain)
            {
                return false;
            }

            for (int x = StartX; x < EndX; x++)
            {
                for (int y = StartY; y < EndY; y++)
                {
                    if (!Pixels[x, y].IsAir() && !Pixels[x, y].IsWater())
                    {
                        Rectangle PixelBounds = new Rectangle(x, y, 1, 1);

                        if (PixelBounds.Intersects(Rect))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void Update(GameTime gameTime)
        {
            Stopwatch sp = new Stopwatch();
            sp.Start();

            if (!ManualUpdate)
            {
                FallingPixels();
            }
            if (sp.ElapsedMilliseconds > LongetsUpdateMs)
            {
                LongetsUpdateMs = sp.Elapsed.Milliseconds;
            }
            sp.Stop();
            //foreach (var item in TerrainSegments)
            //{
            //    item.Value.Renderer.Visible = !Keyboard.GetState().IsKeyDown(Keys.N);
            //}

            if (Keyboard.GetState().IsKeyDown(Keys.U))
            {
                Vector2 P = ContentManager.Game.GetPointer();
                
                int X = (int)P.X;
                int Y = (int)P.Y;
                MakeWater(X, Y);
            }
        }

        public void Draw(GameTime gameTime, GraphicsDevice GDevice)
        {
            if (NoRenderUpdate || !ReadyForRender)
            {
                return;
            }
            if (PixelsChanged.Count > 0)
            {
                RenderTerrainChanges(GDevice);
            }
        }
    }
}
