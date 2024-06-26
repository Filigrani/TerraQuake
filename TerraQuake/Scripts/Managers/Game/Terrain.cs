﻿using TerraQuake;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO.Compression;
using System.Diagnostics;
using System.Net.Cache;
using TinyJSON;
using System.IO;

namespace TerraQuake
{
    internal class Terrain
    {
        public int TerrainW = 2000;
        public int TerrainH = 900;
        public int Seed = 0;

        public TerrainPixel[] Pixels;
        public Color[] TerrainColorData;
        public Color[] LightColorData;
        public Renderer Renderer = null;
        public Renderer RendererBackground = null;
        public Renderer RendererLight = null;
        public GameObject TerrainObject = null;
        public bool ReadyForRender = false;

        public bool UpdateTreadInterrupt = false;
        public bool UpdateThreadsCreated = false;
        public Task UpdateTerrainThread = null;
        public bool TerrainHasBeenModified = false;
        public long LongetsUpdateMs = 0;
        public int LongetsRenderMs = 0;
        public List<TerrainHistoryEvent> TerrainHistory = new List<TerrainHistoryEvent>();
        public int CurrentAge = 0;
        public bool UseHistory = false;
        public List<Chunk> Chunks = new List<Chunk>();
        public ushort ChunksRow = 0;
        public bool ConstantUpdateThread = false;


        //Debug Flags 
        public bool ManualUpdate = false;
        public bool NoRenderUpdate = false;
        public bool AlertNearChunksAnyWay = false;

        public enum ChunksDebug
        {
            None = 0,
            OneColorAlways = 1,
            ManyColorsAlways = 2,
            OneColorWhenUpdated = 3,
            ManyColorsWhenUpdated = 4,
            Count,
        }

        public enum GenerationStyle
        {
            Simple,
            SimpleSnow,
            Solid,
            Count,
        }


        public class Chunk
        {
            public bool RequiredUpdate = false;
            public bool RequiredUpdateNextFrame = false;
            public int StartX = 0;
            public int EndX = 0;
            public int StartY = 0;
            public int EndY = 0;

            public Renderer DebugRenderer = null;

            public Chunk(int x, int y, int endx, int endy)
            {
                StartX= x;
                StartY= y;
                EndX= endx;
                EndY= endy;
            }
        }
        public enum HistoryEventType
        {
            None,
            Hole,
            Snow,
            Count,
        }
        public class TerrainHistoryEvent
        {
            public int Age = 0;
            public HistoryEventType Event = HistoryEventType.None;
            public Point Position = new Point();
            public int Radius = 0;
        }

        public void ResumeHistory()
        {
            UseHistory = true;
        }

        public void AddHoleToHistory(int X, int Y, int Radius, int Age)
        {
            TerrainHistoryEvent E = new TerrainHistoryEvent();
            E.Event = HistoryEventType.Hole;
            E.Age = Age;
            E.Position.X = X; 
            E.Position.Y = Y;
            E.Radius = Radius;
            TerrainHistory.Add(E);

            if (Network.IsHost())
            {
                using (Packet Pack = new Packet((int)PacketsIDs.MAKEHOLE))
                {
                    Pack.Write(X);
                    Pack.Write(Y);
                    Pack.Write(Radius);
                    Pack.Write(Age);
                    Network.GetServer().SendData(Pack);
                }
            }
        }

        public Terrain()
        {
            TerrainObject = GameObjectManager.CreateObject();
            Renderer = new Renderer("Objects");
            RendererBackground = new Renderer("BG");
            RendererBackground.Tint = new Color(167, 167, 167);
            RendererLight = new Renderer("Objects");
            TerrainObject.AddComponent(Renderer, "Main");
            TerrainObject.AddComponent(RendererBackground, "BG");
            TerrainObject.AddComponent(RendererLight, "Light");
            TerrainObject.Position = new Vector2(0, 0);
        }

        public int GetIndex(int x, int y)
        {
            return y * TerrainW + x;
        }

        public TerrainPixel GetPixel(int x, int y)
        {
            int Index = y * TerrainW + x;
            return Pixels[Index];
        }

        public bool IsInsideTerrain(int x, int y)
        {
            if(x <= -1 || x >= TerrainW)
            {
                return false;
            }else if(y <= -1 || y >= TerrainH)
            {
                return false;
            }

            return true;
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
            public int ChunkID = -1;
            public int ChunkLeftID = -1;
            public int ChunkRightID = -1;
            public int ChunkTopID = -1;
            public int ChunkBottomID = -1;
            public void Delete()
            {
                Color = Color.Transparent;
                Rolling = 0;
                RollingDir = 0;
                Fallable = false;
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

        public void AlertChunks(TerrainPixel Px, int iX, int iY)
        {
            int ID = Px.ChunkID;
            int LID = Px.ChunkLeftID;
            int RID = Px.ChunkRightID;
            int TID = Px.ChunkTopID;
            int BID = Px.ChunkBottomID;
            Chunk HomeChunk;
            if (ID != -1)
            {
                HomeChunk = Chunks[ID];
                HomeChunk.RequiredUpdateNextFrame = true;
            } else
            {
                return;
            }
            if(LID != -1)
            {
                if(AlertNearChunksAnyWay || iX == HomeChunk.StartX)
                {
                    Chunks[LID].RequiredUpdateNextFrame = true;
                }
            }
            if (RID != -1)
            {
                if(AlertNearChunksAnyWay || iX == HomeChunk.EndX)
                {
                    Chunks[RID].RequiredUpdateNextFrame = true;
                }
            }
            if (TID != -1)
            {
                if(AlertNearChunksAnyWay || iY == HomeChunk.StartY)
                {
                    Chunks[TID].RequiredUpdateNextFrame = true;
                }
            }
            if (BID != -1)
            {
                if(AlertNearChunksAnyWay || iY == HomeChunk.EndY)
                {
                    Chunks[BID].RequiredUpdateNextFrame = true;
                }
            }
        }
        public void AlertChunks(int iX, int iY)
        {
            AlertChunks(GetPixel(iX, iY), iX, iY);
        }

        public Chunk GetChunk(int iX, int iY)
        {
            return Chunks[GetPixel(iX, iY).ChunkID];
        }

        public void AddChangedPixel(int X, int Y, Color Col)
        {
            int Index = X + TerrainW * Y;
            TerrainColorData[Index] = Col;

            TerrainHasBeenModified = true;
            CurrentAge++;
            OnAgeChanged();
        }

        readonly int[] RandomDirections = { -1, 0, 0, -1, -1, 0, 0, -1, 0, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, 0, -1, -1, 0, 0, 0, -1, 0, -1, 0, 0 };
        public int LastRandomDirectionIndex = 0;

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
        public int LastRandomRollingNumsIndex = 0;

        public int GetRandomRollingNums()
        {
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

        public bool ProcessPixel(int iX, int iY)
        {
            bool Modified = false;
            TerrainPixel Px = GetPixel(iX, iY);
            if (Px.IsFallable()) // Pixel is dynamic.
            {
                if (iY + 1 != TerrainH) // This is not last layer.
                {
                    TerrainPixel PxBelow = GetPixel(iX, iY + 1);
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
                        Px.MoveTo(PxBelow); // Move down
                        Px.Delete(); // Turn current pixel to air.
                        Logger.Log("[Terrain] X "+iX +" Y "+ iY +" -> X "+iX +" Y "+ iY +1);

                        // Re-render this pixels
                        AddChangedPixel(iX, iY, Color.Transparent);
                        AddChangedPixel(iX, iY + 1, PxBelow.Color);
                        AlertChunks(Px, iX, iY);
                        AlertChunks(PxBelow, iX, iY + 1);
                        Modified = true;
                        //return; // Can't rolling this frame if fall.
                    }
                }

                if ((Px.Water && Px.Rolling > -1000) || Px.Rolling > 0) // Ability to roll left and right.
                {
                    Px.Rolling--; // Remove 1 from rolling counts.

                    bool CanRollRight = false;
                    bool CanRollLeft = false;
                    TerrainPixel PxRight = null;
                    TerrainPixel PxLeft = null;
                    if (iX + 1 < TerrainW)
                    {
                        PxRight = GetPixel(iX + 1, iY);
                        CanRollRight = PxRight.IsAir();
                    }
                    if (iX - 1 >= 0)
                    {
                        PxLeft = GetPixel(iX - 1, iY);
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
                    int Dir = Px.RollingDir;

                    if (Dir != 0)
                    {
                        if (Dir == 1)
                        {
                            Px.MoveTo(PxRight);
                            Px.Delete();
                            Logger.Log("[Terrain] X " + iX + " Y " + iY + " -> X " + iX + Dir + " Y " + iY);

                            AddChangedPixel(iX + Dir, iY, PxRight.Color);
                            AddChangedPixel(iX, iY, Color.Transparent);
                            AlertChunks(Px, iX, iY);
                            AlertChunks(PxRight, iX + Dir, iY);
                            Modified = true;
                        } else
                        {
                            Px.MoveTo(PxLeft);
                            Px.Delete();
                            Logger.Log("[Terrain] X " + iX + " Y " + iY + " -> X " + iX + Dir + " Y " + iY);

                            AddChangedPixel(iX + Dir, iY, PxLeft.Color);
                            AddChangedPixel(iX, iY, Color.Transparent);
                            AlertChunks(Px, iX, iY);
                            AlertChunks(PxLeft, iX + Dir, iY);
                            Modified = true;
                        }
                    }
                }
            }
            return Modified;
        }


        //public const int PixelMoveSpeed = 1;
        //public void ProcessPixel(int iX, int iY, bool ScanRight = false)
        //{
        //    TerrainPixel Px = GetPixel(iX, iY);
        //    if (Px.IsFallable()) // Pixel is dynamic.
        //    {
        //        int Fall = 0;
        //        bool CanRoll = false;
        //        while (Fall != PixelMoveSpeed)
        //        {
        //            if (iY + 1 != TerrainH) // This is not last layer.
        //            {
        //                TerrainPixel PxBelow = GetPixel(iX, iY + 1);
        //                if (PxBelow.IsAir()) // Air below can fall down.
        //                {
        //                    if (Px.Rolling == 0)
        //                    {
        //                        if (!Px.IsWater())
        //                        {
        //                            Px.Rolling = GetRandomRollingNums();
        //                        } else
        //                        {
        //                            Px.Rolling = 0;
        //                        }
        //                    }
        //                    Px.MoveTo(PxBelow); // Move down
        //                    Px.Delete(); // Turn current pixel to air.

        //                    // Re-render this pixels
        //                    AddChangedPixel(iX, iY, Color.Transparent);
        //                    AddChangedPixel(iX, iY + 1, PxBelow.Color);
        //                    AddProcessPixel(Px);
        //                    AddProcessPixel(PxBelow);
        //                    Fall++;
        //                    Px = PxBelow;
        //                    iY++;
        //                    continue;
        //                } else
        //                {
        //                    CanRoll = true;
        //                    break;
        //                }
        //            } else
        //            {
        //                CanRoll = true;
        //                break;
        //            }
        //        }

        //        if (CanRoll)
        //        {
        //            int Roll = 0;
        //            while(Roll != PixelMoveSpeed)
        //            {
        //                if ((Px.Water && Px.Rolling > -1000) || Px.Rolling > 0) // Ability to roll left and right.
        //                {
        //                    Px.Rolling--; // Remove 1 from rolling counts.

        //                    bool CanRollRight = false;
        //                    bool CanRollLeft = false;
        //                    TerrainPixel PxRight = null;
        //                    TerrainPixel PxLeft = null;
        //                    if (iX + 1 < TerrainW)
        //                    {
        //                        PxRight = GetPixel(iX + 1, iY);
        //                        CanRollRight = PxRight.IsAir();
        //                    }
        //                    if (iX - 1 >= 0)
        //                    {
        //                        PxLeft = GetPixel(iX - 1, iY);
        //                        CanRollLeft = PxLeft.IsAir();
        //                    }

        //                    if (CanRollLeft && CanRollRight)
        //                    {
        //                        if (ScanRight)
        //                        {
        //                            Px.RollingDir = -1;
        //                        } else
        //                        {
        //                            Px.RollingDir = 1;
        //                        }
        //                    } else if (CanRollLeft)
        //                    {
        //                        Px.RollingDir = -1;
        //                    } else if (CanRollRight)
        //                    {
        //                        Px.RollingDir = 1;
        //                    } else
        //                    {
        //                        Px.Rolling = 0;
        //                        Px.RollingDir = 0;
        //                    }
        //                    int Dir = Px.RollingDir;

        //                    if (Dir != 0)
        //                    {
        //                        if (Dir == 1)
        //                        {
        //                            Px.MoveTo(PxRight);
        //                            Px.Delete();
        //                            AddChangedPixel(iX + Dir, iY, PxRight.Color);
        //                            AddChangedPixel(iX, iY, Color.Transparent);
        //                            AddProcessPixel(Px);
        //                            AddProcessPixel(PxRight);
        //                            Px = PxRight;
        //                            iX ++;
        //                        } else
        //                        {
        //                            Px.MoveTo(PxLeft);
        //                            Px.Delete();
        //                            AddChangedPixel(iX + Dir, iY, PxLeft.Color);
        //                            AddChangedPixel(iX, iY, Color.Transparent);
        //                            AddProcessPixel(Px);
        //                            AddProcessPixel(PxLeft);
        //                            Px = PxLeft;
        //                            iX--;
        //                        }
        //                        Roll++;
        //                    } else
        //                    {
        //                        break;
        //                    }
        //                } else
        //                {
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}

        public bool ScanDirectionRight = false;

        public void FallingPixels()
        {
            Stopwatch sp = new Stopwatch();
            while (!UpdateTreadInterrupt)
            {
                sp.Restart();
                for (int iY = TerrainH - 1; iY != -1; iY--)
                {
                    if (ScanDirectionRight)
                    {
                        for (int iX = 0; iX != TerrainW; iX++)
                        {
                            ProcessPixel(iX, iY);
                        }
                    } else
                    {
                        for (int iX = TerrainW - 1; iX != 1; iX--)
                        {
                            ProcessPixel(iX, iY);
                        }
                    }
                }
                ScanDirectionRight = !ScanDirectionRight;

                if (sp.ElapsedMilliseconds > LongetsUpdateMs)
                {
                    LongetsUpdateMs = sp.Elapsed.Milliseconds;
                }
                sp.Stop();
            }
        }

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

        public Color GetSnowColor()
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
                return Color.Snow;
            } else if (Variant == 4)
            {
                return Color.White;
            } else if (Variant == 5)
            {
                return Color.GhostWhite;
            } else if (Variant == 6)
            {
                return Color.Lavender;
            }
            return Color.SlateGray;
        }

        public Color GetSnowGrassColor()
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
            Texture2D texture = ContentManager.GetSprite("DebugWhite");
            Color[] TextureData = new Color[texture.Width * texture.Height];
            texture.GetData(TextureData);
            Pixels = new TerrainPixel[TerrainW * TerrainH];

            for (int i = 0; i != TextureData.Length; i++)
            {
                Pixels[i] = new TerrainPixel();
                Pixels[i].Color = TextureData[i];
            }
            RenderTerrain();
        }

        public void HillsGenerator()
        {
            for (int i = 0; i != Pixels.Length; i++)
            {
                Pixels[i] = new TerrainPixel();
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

                GetPixel(iX, HillHeigh).Color = GetGrassColor();
                int UnitsPlaced = 0;
                for (int iY = HillHeigh; iY != TerrainH; iY++)
                {
                    UnitsPlaced++;

                    TerrainPixel Px = GetPixel(iX, iY);

                    if (UnitsPlaced <= 45)
                    {
                        Px.Color = GetGrassColor();
                    } else if (UnitsPlaced > 45 && UnitsPlaced < 145)
                    {
                        Px.Color = GetGroundColor();
                        if(UnitsPlaced > 60 + WorldGenRandom.Next(0, 20))
                        {
                            Px.HasBackground = true;
                        }
                    } else
                    {
                        Px.Color = GetStoneColor();
                        Px.Fallable = false;
                        Px.HasBackground = true;
                    }
                }
            }
            RenderTerrainBackground();
        }

        public void AdvancedGenerator()
        {
            for (int i = 0; i != Pixels.Length; i++)
            {
                Pixels[i] = new TerrainPixel();
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

                GetPixel(iX, HillHeigh).Color = GetGrassColor();
                int UnitsPlaced = 0;
                for (int iY = HillHeigh; iY != TerrainH; iY++)
                {
                    UnitsPlaced++;

                    TerrainPixel Px = GetPixel(iX, iY);
                    if (UnitsPlaced < 40)
                    {
                        Px.Color = GetGrassColor();
                    } else if (UnitsPlaced > 40 && UnitsPlaced < 140)
                    {
                        Px.Color = GetGroundColor();
                    } else
                    {
                        Px.Color = GetStoneColor();
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
                    TerrainPixel Px = Pixels[GetIndex(iX, iY)] = new TerrainPixel();
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

            RenderTerrainBackground();
        }
        public void SolidGeneratorFallable()
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                TerrainPixel Px = Pixels[i] = new TerrainPixel();
                Px.Color = Color.AliceBlue;
                Px.HasBackground = true;
            }

            RenderTerrainBackground();
        }

        public void SimpleGeneratorSnow()
        {
            for (int iY = 0; iY < TerrainH; iY++)
            {
                for (int iX = 0; iX < TerrainW; iX++)
                {
                    TerrainPixel Px = Pixels[GetIndex(iX, iY)] = new TerrainPixel();

                    if(iY > 100)
                    {
                        if (iY < 280)
                        {
                            if (iY <= 170 + WorldGenRandom.Next(0, 10))
                            {
                                Px.Color = GetSnowGrassColor();
                            } else
                            {
                                Px.Color = GetGroundColor();
                                if (iY > 170 + WorldGenRandom.Next(0, 10))
                                {
                                    Px.HasBackground = true;
                                }
                            }
                        } else if (iY >= 280 + WorldGenRandom.Next(0, 25))
                        {
                            Px.Color = GetStoneColor();
                            Px.Fallable = false;
                            Px.HasBackground = true;

                            if(iY < 305)
                            {
                                Px.Fallable = true;
                            }
                        } else
                        {
                            Px.Color = GetGroundColor();
                            Px.HasBackground = true;
                        }
                    } else
                    {
                        Px.Color = Color.Transparent;
                    }
                }
            }

            RenderTerrainBackground();

            for (int i = 1; i != 20; i++)
            {
                AddPond(WorldGenRandom, 120);
            }
            for (int i = 1; i != 20; i++)
            {
                AddOre(WorldGenRandom, 305);
            }
        }

        public void SimpleGenerator()
        {
            for (int iY = 0; iY < TerrainH; iY++)
            {
                for (int iX = 0; iX < TerrainW; iX++)
                {
                    TerrainPixel Px = Pixels[GetIndex(iX, iY)] = new TerrainPixel();
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

            RenderTerrainBackground();

            for (int i = 1; i != 20; i++)
            {
                AddPond(WorldGenRandom);
            }
            for (int i = 1; i != 20; i++)
            {
                AddOre(WorldGenRandom, 105);
            }
        }

        public void LocateChunks()
        {
            Color[] Colors = { Color.Red, Color.Green, Color.Yellow, Color.Blue, Color.Cyan, Color.Salmon };
            bool OneColor = Settings.ChunkDebugMode == ChunksDebug.OneColorAlways || Settings.ChunkDebugMode == ChunksDebug.OneColorWhenUpdated;
            bool DebugColors = Settings.ChunkDebugMode != ChunksDebug.None;
            int ColorIndex = 0;
            Texture2D DebugTex = ContentManager.GetSprite("DebugWhite");
            Chunks.Clear();
            int ChunkW = 20;
            int ChunkH = 20;
            int X = 0;
            int Y = 0;
            ChunksRow = (ushort) Math.Ceiling((double)TerrainW / ChunkW);
            while (true)
            {
                int EndX = X + ChunkW - 1;
                int EndY = Y + ChunkH - 1;

                if(EndX >= TerrainW)
                {
                    EndX = TerrainW - 1;
                }
                if (EndY >= TerrainH)
                {
                    EndX = TerrainH - 1;
                }

                if(Y > TerrainH-1)
                {
                    break;
                }

                if(X > TerrainW-1)
                {
                    Y = EndY+1;
                    X = 0;
                    continue;
                }
                Chunk C = new Chunk(X, Y, EndX, EndY);

                if (DebugColors)
                {
                    GameObject ChunkDebug = GameObjectManager.CreateObject();
                    Renderer Rend = new Renderer("Debug");
                    Rend.SetSprite(DebugTex, new Rectangle(0, 0, EndX - X + 1, EndY - Y + 1));
                    if (OneColor)
                    {
                        Rend.Tint = Color.Aqua;
                    } else
                    {
                        ColorIndex++;
                        if (ColorIndex == Colors.Length)
                        {
                            ColorIndex = 0;
                        }
                        Rend.Tint = Colors[ColorIndex];
                    }
                    ChunkDebug.AddComponent(Rend);
                    ChunkDebug.Position = new Vector2(X, Y);
                    C.DebugRenderer = Rend;
                }

                Chunks.Add(C);

                int ChunkID = Chunks.Count - 1;
                for (int iY = Y; iY <= EndY; iY++)
                {
                    for (int iX = X; iX <= EndX; iX++)
                    {
                        TerrainPixel Px = GetPixel(iX, iY);
                        Px.ChunkID = ChunkID;
                        if(X != 0)
                        {
                            Px.ChunkLeftID = ChunkID - 1;
                        }
                        if(EndX != TerrainW-1)
                        {
                            Px.ChunkRightID = ChunkID + 1;
                        }
                        if(Y != 0)
                        {
                            Px.ChunkTopID = ChunkID - ChunksRow;
                        }
                        if(EndY != TerrainH-1)
                        {
                            Px.ChunkBottomID = ChunkID + ChunksRow;
                        }
                    }
                }
                X = EndX+1;
            }

            for (int iY = TerrainH-1; iY != -1; iY--)
            {
                for (int iX = 0; iX != TerrainH-1; iX++)
                {
                    TerrainPixel Px = GetPixel(iX, iY);
                    if(Px.ChunkID == -1)
                    {
                        int WhatTheHeck = 0;
                    }
                }
            }
        }


        public void CreateTerrain(int RNGSeed = 0)
        {
            Seed = RNGSeed;


            if (UpdateThreadsCreated)
            {
                UpdateTreadInterrupt = true;
                UpdateTerrainThread.Wait();
                if (UpdateTerrainThread.IsCompleted)
                {
                    UpdateTerrainThread.Dispose();
                }
                UpdateThreadsCreated = false;
                UpdateTreadInterrupt = false;
            }
            
            
            ReadyForRender = false;
            if (Seed == 0)
            {
                Seed = Guid.NewGuid().GetHashCode();
            }
            ShuffleRandom = new Random(Seed);
            WorldGenRandom = new Random(Seed);
            Pixels = new TerrainPixel[TerrainW * TerrainH];

            switch (Settings.TerrainStyle)
            {
                case GenerationStyle.Simple:
                    SimpleGenerator();
                    break;
                case GenerationStyle.SimpleSnow:
                    SimpleGeneratorSnow();
                    break;
                case GenerationStyle.Solid:
                    SolidGenerator();
                    break;
                default:
                    SolidGenerator();
                    break;
            }

            //AdvancedGenerator();
            //HillsGenerator();
            //SolidGenerator();
            //SolidGeneratorFallable();
            LocateChunks();
            ReadyForRender = true;
            RenderTerrain();
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

        public void ApplyExplosionEffect(int X, int Y, int Radius)
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
        }


        public void MakeHole(int X, int Y, int Radius, bool FromHistory = false)
        {
            int age = CurrentAge;
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
                    double Dis = Math.Sqrt(num * num + num2 * num2);


                    if (Dis <= Radius)
                    {
                        TerrainPixel Px = GetPixel(iX, iY);
                        Px.Delete();
                        AddChangedPixel(iX, iY, Color.Transparent);

                        if(iY -1 != -1) // Something can be above.
                        {
                            int num3 = iX - X;
                            int num4 = (iY-1) - Y;
                            double DisAbovPx = Math.Sqrt(num3 * num3 + num4 * num4);

                            if(DisAbovPx > Radius) // Distance check failed, this is edge pixel!
                            {
                                if(GetPixel(iX, iY - 1).IsFallable())
                                {
                                    AlertChunks(Px, iX, iY);
                                }
                            }
                        }
                    }
                }
            }
            //AddProcessArea(new Rectangle(StartX, StartY, EndX- StartX, EndY-StartY));
            if (!FromHistory)
            {
                AddHoleToHistory(X, Y, Radius, age);
            }
        }

        public void MakeWater(int X, int Y)
        {
            if (X < 0 || X > TerrainW-1 || Y > TerrainH-1 || Y < 0)
            {
                return;
            }
            TerrainPixel Px = GetPixel(X, Y);
            Px.Fallable = true;
            Px.Water = true;
            Px.Color = GetWaterColor();
            AlertChunks(Px, X, Y);
        }

        public void MakeSnow(int X, int Y, int Radius)
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
                        Color Col = GetSnowColor();
                        TerrainPixel Px = GetPixel(iX, iY);
                        Px.Fallable = true;
                        Px.Color = Col;
                        AddChangedPixel(iX, iY, Col);
                        AlertChunks(Px, iX, iY);
                    }
                }
            }
        }

        public void MakeWater(int X, int Y, int Radius)
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
                        Color Col = GetWaterColor();
                        TerrainPixel Px = GetPixel(iX, iY);
                        Px.Fallable = true;
                        Px.Water = true;
                        Px.Color = Col;
                        AddChangedPixel(iX, iY, Col);
                        AlertChunks(Px, iX, iY);
                    }
                }
            }
        }

        public void AddPond(Random RNG, int HighestPond = -1)
        {
            int PondCenterX = RNG.Next(5, TerrainW);
            int PondCenterY;

            if(HighestPond != -1)
            {
                PondCenterY = RNG.Next(HighestPond, TerrainH);
            } else
            {
                PondCenterY = RNG.Next(5, TerrainH);
            }

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
                            TerrainPixel Px = GetPixel(iX, iY);
                            Px.Color = GetWaterColor();
                            Px.Water = true;
                        }
                    }
                }
            }
        }

        public void AddOre(Random RNG, int HighestMine = -1)
        {
            int OreCenterX = RNG.Next(5, TerrainW);
            int OreCenterY;

            if (HighestMine != -1 && HighestMine < TerrainH)
            {
                OreCenterY = RNG.Next(HighestMine, TerrainH);
            } else
            {
                OreCenterY = RNG.Next(5, TerrainH);
            }

            Color OreColor = Color.Gold;
            int OreChance = 3;
            int OreRadiusMin = 5;
            int OreRadiusMax = 7;

            int Chance = RNG.Next(0, 100);
            if (Chance < 20)
            {
                OreColor = Color.Aqua;
                OreChance = 20;
                OreRadiusMin = 2;
                OreRadiusMax = 5;
            } else if(Chance < 30)
            {
                OreColor = Color.Gold;
                OreChance = 15;
                OreRadiusMin = 5;
                OreRadiusMax = 7;
            } else if (Chance < 40)
            {
                OreColor = Color.PaleGreen;
                OreChance = 20;
                OreRadiusMin = 3;
                OreRadiusMax = 5;
            } else if (Chance < 60)
            {
                OreColor = Color.Lavender;
                OreChance = 30;
                OreRadiusMin = 3;
                OreRadiusMax = 7;
            } else
            {
                OreColor = Color.Black;
                OreChance = 30;
                OreRadiusMin = 5;
                OreRadiusMax = 7;
            }


            int Childs = 21;
            int MaxDistance = 20;
            for (int i = 1; i != Childs; i++)
            {
                int ChildX = OreCenterX - RNG.Next(-MaxDistance, MaxDistance);
                int ChildY = OreCenterY - RNG.Next(-MaxDistance, MaxDistance);

                int Radius = RNG.Next(OreRadiusMin, OreRadiusMax);
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
                        if(RNG.Next(0, 100) <= OreChance)
                        {
                            int num = iX - ChildX;
                            int num2 = iY - ChildY;
                            float Dis = MathF.Sqrt(num * num + num2 * num2);
                            if (Dis <= Radius)
                            {
                                TerrainPixel Px = GetPixel(iX, iY);

                                if(!Px.IsWater() && !Px.IsFallable() && !Px.IsAir())
                                {
                                    Px.Color = OreColor;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RenderTerrainChanges()
        {
            if (TerrainColorData == null)
            {
                TerrainColorData = new Color[TerrainW * TerrainH];
            }
            Renderer.Sprite.SetData(TerrainColorData);
        }

        public void RenderTerrainBackground()
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
                if (Pixels[Index].HasBackground)
                {
                    BackgroundColorData[Index] = Pixels[Index].Color;
                }
                Index++;
                ColorX++;
            }
            RendererBackground.SetSprite(BackgroundColorData, TerrainW, TerrainH);
        }

        public void RenderTerrain()
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
                Color PixelColor = Pixels[Index].Color;
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
        public bool CheckCollision(Rectangle Rect, bool IgnoreWater = true)
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
                    TerrainPixel Px = GetPixel(x, y);
                    if (!Px.IsAir() && (!IgnoreWater || !Px.IsWater()))
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

        public void OnAgeChanged()
        {
            if (UseHistory)
            {
                foreach (TerrainHistoryEvent Event in TerrainHistory)
                {
                    if (Event.Age == CurrentAge)
                    {
                        if (Event.Event == HistoryEventType.Hole)
                        {
                            MakeHole(Event.Position.X, Event.Position.Y, Event.Radius, true);
                        }
                    }
                }
            }
        }

        public void ProcessAllChunks()
        {
            bool CanSwapScan = false;
            OnAgeChanged();

            Chunk C;
            for (int i = 0; i != Chunks.Count; i++)
            {
                C = Chunks[i];
                C.RequiredUpdate = C.RequiredUpdateNextFrame;
                C.RequiredUpdateNextFrame = false;
            }
            TerrainPixel Px;
            for (int iY = TerrainH - 1; iY != -1; iY--)
            {
                if (ScanDirectionRight)
                {
                    for (int iX = 0; iX != TerrainW; iX++)
                    {
                        Px = GetPixel(iX, iY);
                        C = Chunks[Px.ChunkID];

                        if(C.RequiredUpdate)
                        {
                            bool Modified = ProcessPixel(iX, iY);
                            if (Modified)
                            {
                                CanSwapScan = true;
                            }
                        } else
                        {
                            if (C.EndX != TerrainW - 1)
                            {
                                iX = C.EndX + 1;
                            } else
                            {
                                break;
                            }
                        }
                    }
                } else
                {
                    for (int iX = TerrainW - 1; iX != 1; iX--)
                    {
                        Px = GetPixel(iX, iY);
                        C = Chunks[Px.ChunkID];

                        if (C.RequiredUpdate)
                        {
                            bool Modified = ProcessPixel(iX, iY);
                            if (Modified)
                            {
                                CanSwapScan = true;
                            }
                        } else
                        {
                            if (C.StartX != 0)
                            {
                                iX = C.StartX - 1;
                            } else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (CanSwapScan)
            {
                ScanDirectionRight = !ScanDirectionRight;
            }
        }

        public void UpdateChunks()
        {
            while (!UpdateTreadInterrupt)
            {
                ProcessAllChunks();
            }
        }

        public void Update()
        {
            bool Always = Settings.ChunkDebugMode == ChunksDebug.OneColorAlways || Settings.ChunkDebugMode == ChunksDebug.ManyColorsAlways;
            bool DebugColors = Settings.ChunkDebugMode != ChunksDebug.None;

            if (DebugColors)
            {
                foreach (Chunk C in Chunks)
                {
                    if (Always)
                    {
                        C.DebugRenderer.Visible = true;
                    } else
                    {
                        C.DebugRenderer.Visible = C.RequiredUpdate || C.RequiredUpdateNextFrame;
                    }
                }
            }

            if (!ManualUpdate && ReadyForRender)
            {
                if (ConstantUpdateThread)
                {
                    if (!UpdateThreadsCreated)
                    {
                        UpdateTerrainThread = Task.Factory.StartNew(UpdateChunks);
                        UpdateThreadsCreated = true;
                    }
                } else
                {
                    if (!UpdateThreadsCreated || (UpdateThreadsCreated && (UpdateTerrainThread == null || UpdateTerrainThread.IsCompleted)))
                    {
                        if (UpdateTerrainThread != null)
                        {
                            UpdateTerrainThread.Dispose();
                            UpdateTerrainThread = null;
                        }
                        UpdateThreadsCreated = true;
                        UpdateTerrainThread = Task.Factory.StartNew(ProcessAllChunks);
                    }
                }
            }

            if (Input.KeyDown(Keys.U))
            {
                Vector2 P = ContentManager.Game.GetPointer();
                
                int X = (int)P.X;
                int Y = (int)P.Y;
                MakeWater(X, Y, 3);
            }
            if (Input.KeyPressed(Keys.T))
            {
                Vector2 P = ContentManager.Game.GetPointer();
                int X = (int)P.X;
                int Y = (int)P.Y;

                MakeHole(X, Y, 40);
            }
            if (Input.KeyPressed(Keys.B))
            {
                LongetsUpdateMs = 0;
                BenchHoles();
            }
            if (Input.KeyPressed(Keys.R))
            {
                CreateTerrain();
            }
            if (Input.KeyPressed(Keys.Y))
            {
                Vector2 P = ContentManager.Game.GetPointer();
                int X = (int)P.X;
                int Y = (int)P.Y;

                MakeSnow(X, Y, 10);
            }
            if (Input.KeyPressed(Keys.LeftShift))
            {
                if (ManualUpdate)
                {
                    if (!UpdateThreadsCreated || (UpdateThreadsCreated && (UpdateTerrainThread == null || UpdateTerrainThread.IsCompleted)))
                    {
                        if (UpdateTerrainThread != null)
                        {
                            UpdateTerrainThread.Dispose();
                            UpdateTerrainThread = null;
                        }
                        UpdateThreadsCreated = true;
                        UpdateTerrainThread = Task.Factory.StartNew(ProcessAllChunks);
                    }
                }
            }
        }

        public static List<T> CloneList<T>(List<T> original)
        {
            List<T> clone = new List<T>();

            lock (original)
            {
                foreach (T item in original)
                {
                    clone.Add(item);
                }
            }

            return clone;
        }

        public float ProcentFromDistance(float Distance, float MaxDistance)
        {
            return ((100f * Distance) / MaxDistance);
        }

        //  Method by DavidMcLaughlin. Ported from Java to C# by Me.
        // https://gist.github.com/DavidMcLaughlin208/60e69e698e3858617c322d80a8f174e2
        public void TwoPoints(Point pos1, Point pos2, int Radius)
        {
            if (pos1.Equals(pos2))
            {

            }

            int matrixX1 = (int)pos1.X;
            int matrixY1 = (int)pos1.Y;
            int matrixX2 = (int)pos2.X;
            int matrixY2 = (int)pos2.Y;

            int xDiff = matrixX1 - matrixX2;
            int yDiff = matrixY1 - matrixY2;
            bool xDiffIsLarger = Math.Abs(xDiff) > Math.Abs(yDiff);

            int xModifier = xDiff < 0 ? 1 : -1;
            int yModifier = yDiff < 0 ? 1 : -1;

            int longerSideLength = Math.Max(Math.Abs(xDiff), Math.Abs(yDiff));
            int shorterSideLength = Math.Min(Math.Abs(xDiff), Math.Abs(yDiff));
            float slope = (shorterSideLength == 0 || longerSideLength == 0) ? 0 : ((float)(shorterSideLength) / (longerSideLength));
            int shorterSideIncrease;
            for (int i = 1; i <= longerSideLength; i++)
            {
                shorterSideIncrease = (int) Math.Round((double)i * slope);
                int yIncrease, xIncrease;
                if (xDiffIsLarger)
                {
                    xIncrease = i;
                    yIncrease = shorterSideIncrease;
                } else
                {
                    yIncrease = i;
                    xIncrease = shorterSideIncrease;
                }
                int currentY = matrixY1 + (yIncrease * yModifier);
                int currentX = matrixX1 + (xIncrease * xModifier);
                if (IsInsideTerrain(currentX, currentY))
                {
                    Point pos3 = new Point(currentX, currentY);

                }
            }
            return;
        }


        public void Draw(GameTime gameTime, GraphicsDevice GDevice)
        {
            if (NoRenderUpdate || !ReadyForRender)
            {
                return;
            }
            if (TerrainHasBeenModified)
            {
                RenderTerrainChanges();
                TerrainHasBeenModified = false;
            }
        }

        //public static string CompressString(string text)
        //{
        //    byte[] buffer = Encoding.UTF8.GetBytes(text);
        //    var memoryStream = new MemoryStream();
        //    using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        //    {
        //        gZipStream.Write(buffer, 0, buffer.Length);
        //    }

        //    memoryStream.Position = 0;

        //    var compressedData = new byte[memoryStream.Length];
        //    memoryStream.Read(compressedData, 0, compressedData.Length);

        //    var gZipBuffer = new byte[compressedData.Length + 4];
        //    Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
        //    Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
        //    return Convert.ToBase64String(gZipBuffer);
        //}

        //public static string DecompressString(string compressedText)
        //{
        //    byte[] gZipBuffer = Convert.FromBase64String(compressedText);
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
        //        memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

        //        var buffer = new byte[dataLength];

        //        memoryStream.Position = 0;
        //        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
        //        {
        //            gZipStream.Read(buffer, 0, buffer.Length);
        //        }

        //        return Encoding.UTF8.GetString(buffer);
        //    }
        //}
    }
}
