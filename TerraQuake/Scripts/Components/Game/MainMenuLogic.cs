using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace TerraQuake
{
    internal class MainMenuLogic : Component
    {
        public SpriteFont Texter = null;
        public int MenuIndex = 0;
        public int SelectedElement = 0;
        public int Elements = 0;
        public string LevelToLoad = "Game";

        public MainMenuLogic() 
        {
            Texter = new SpriteFont();
            Texter.Font = ContentManager.GetSprite("DebugFont");
            OpenMenu(0);
        }

        public void OpenMenu(int Index)
        {
            MenuIndex = Index;
            SelectedElement = 0;
            if (Index == 0)
            {
                Elements = 2;
            }else if(Index == 1)
            {
                Elements = 6;
            } else if (Index == 2)
            {
                Elements = 3;
            } else if (Index == 3)
            {
                Elements = 0;
            }
        }


        public void ProcessControls()
        {
            if (Input.KeyPressed(Keys.Up) || Input.KeyPressed(Keys.W))
            {
                if(SelectedElement > 0)
                {
                    SelectedElement--;
                }
            }
            else if (Input.KeyPressed(Keys.Down) || Input.KeyPressed(Keys.S))
            {
                if (SelectedElement < Elements)
                {
                    SelectedElement++;
                }
            }
            else if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A))
            {
                if(MenuIndex == 0)
                {
                    if(SelectedElement == 1)
                    {
                        OpenMenu(1);
                    } else if(SelectedElement == 0)
                    {
                        OpenMenu(2);
                    } else if (SelectedElement == 2)
                    {
                        ContentManager.Game.Exit();
                    }
                } else if(MenuIndex == 1)
                {
                    if (SelectedElement == 6)
                    {
                        OpenMenu(0);
                    }else if(SelectedElement == 1)
                    {
                        if(Settings.ChunkDebugMode != 0)
                        {
                            Settings.ChunkDebugMode = Settings.ChunkDebugMode - 1;
                        }
                    } else if (SelectedElement == 4)
                    {
                        if (Settings.Weather != 0)
                        {
                            Settings.Weather = Settings.Weather - 1;
                        }
                    } else if (SelectedElement == 5)
                    {
                        if (Settings.TerrainStyle != 0)
                        {
                            Settings.TerrainStyle = Settings.TerrainStyle - 1;
                        }
                    } else if (SelectedElement == 2)
                    {
                        if(Settings.DefaultTerrainW > 100)
                        {
                            Settings.DefaultTerrainW -= 100;
                        }
                    } else if (SelectedElement == 3)
                    {
                        if (Settings.DefaultTerrainH > 100)
                        {
                            Settings.DefaultTerrainH -= 100;
                        }
                    }
                } else if (MenuIndex == 2)
                {
                    if (SelectedElement == 0)
                    {
                        LevelManager.StartLevel(LevelToLoad);
                    } else if (SelectedElement == 1)
                    {
                        Network.StartServer();
                        LevelManager.StartLevel(LevelToLoad);
                    } else if (SelectedElement == 2)
                    {
                        Network.StartClient();
                        OpenMenu(3);
                    }else if(SelectedElement == 3)
                    {
                        OpenMenu(0);
                    }
                }
            }
            else if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D))
            {
                if (MenuIndex == 0)
                {
                    if (SelectedElement == 1)
                    {
                        OpenMenu(1);
                    } else if (SelectedElement == 0)
                    {
                        OpenMenu(2);
                    } else if (SelectedElement == 2)
                    {
                        ContentManager.Game.Exit();
                    }
                } else if (MenuIndex == 1)
                {
                    if (SelectedElement == 6)
                    {
                        OpenMenu(0);
                    } else if (SelectedElement == 1)
                    {
                        if (Settings.ChunkDebugMode != Terrain.ChunksDebug.Count-1)
                        {
                            Settings.ChunkDebugMode = Settings.ChunkDebugMode + 1;
                        }
                    } else if (SelectedElement == 4)
                    {
                        if (Settings.Weather != WeatherParticles.ImpactMaterial.Count - 1)
                        {
                            Settings.Weather = Settings.Weather + 1;
                        }
                    } else if (SelectedElement == 5)
                    {
                        if (Settings.TerrainStyle != Terrain.GenerationStyle.Count - 1)
                        {
                            Settings.TerrainStyle = Settings.TerrainStyle + 1;
                        }
                    } else if (SelectedElement == 0)
                    {
                        if (ContentManager.Game.WindowWidth == 960)
                        {
                            ContentManager.Game.ApplyReolustion(1920, 1080, false);
                            LayersManager.Scaler = 2;
                            LayersManager.ScrollOffset = Vector2.Zero;
                            ContentManager.Game.ApplyChanges();
                        }else if (ContentManager.Game.WindowWidth == 2560)
                        {
                            ContentManager.Game.ApplyReolustion(960, 540, false);
                            LayersManager.Scaler = 1;
                            LayersManager.ScrollOffset = Vector2.Zero;
                            ContentManager.Game.ApplyChanges();
                        } else if (ContentManager.Game.WindowWidth == 1920)
                        {
                            ContentManager.Game.ApplyReolustion(1024, 768, false);
                            LayersManager.Scaler = 1;
                            LayersManager.ScrollOffset = new Vector2(32, 114);
                            ContentManager.Game.ApplyChanges();
                        } else if (ContentManager.Game.WindowWidth == 1024)
                        {
                            ContentManager.Game.ApplyReolustion(2560, 1080, false);
                            LayersManager.Scaler = 3;
                            LayersManager.ScrollOffset = new Vector2(32, 114);
                            ContentManager.Game.ApplyChanges();
                        }
                    } else if (SelectedElement == 2)
                    {
                        Settings.DefaultTerrainW += 100;
                    } else if (SelectedElement == 3)
                    {
                        Settings.DefaultTerrainH += 200;
                    }
                }else if(MenuIndex == 2)
                {
                    if (SelectedElement == 0)
                    {
                        LevelManager.StartLevel(LevelToLoad);
                    }else if(SelectedElement == 1)
                    {
                        Network.StartServer();
                        LevelManager.StartLevel(LevelToLoad);
                    } else if(SelectedElement == 2)
                    {
                        Network.StartClient();
                        OpenMenu(3);
                        Network.GetClient().Connect("127.0.0.1", Network.DefaultPort);
                    } else if (SelectedElement == 3)
                    {
                        OpenMenu(0);
                    }
                }
            }
        }

        public string SelectMarker(int Index)
        {
            if(SelectedElement == Index)
            {
                return "> ";
            } else
            {
                return "";
            }
        }


        public override void Update(GameTime gameTime)
        {
            ProcessControls();
            string MenuText = "";
            if(Texter != null)
            {
                MenuText = "\nSimple Menu" + "\n";

                if(MenuIndex == 0)
                {
                    MenuText += "\n" + SelectMarker(0) + "Start"
                    + "\n" + SelectMarker(1) + "Settings"
                    + "\n" + SelectMarker(2) + "Exit";
                } else if(MenuIndex == 1)
                {
                    MenuText += "\n" + "Video" + "\n"
                    + "\n" + SelectMarker(0) + "Resolution " + ContentManager.Game.WindowWidth + "x" + ContentManager.Game.WindowHeight
                    + "\n" + SelectMarker(1) + "Chunk Debug " + Settings.ChunkDebugMode.ToString()

                    +"\n\n" + "Terrain" + "\n"

                    + "\n" + SelectMarker(2) + "Width " + Settings.DefaultTerrainW
                    + "\n" + SelectMarker(3) + "Height " + Settings.DefaultTerrainH
                    + "\n" + SelectMarker(4) + "Weather " + Settings.Weather.ToString()
                    + "\n" + SelectMarker(5) + "Style " + Settings.TerrainStyle.ToString()

                    + "\n\n" + SelectMarker(6) + "Back";
                } else if (MenuIndex == 2)
                {
                    MenuText += "\n" + "Mode" + "\n"
                    + "\n" + SelectMarker(0) + "Singleplayer"
                    + "\n" + SelectMarker(1) + "Host"
                    + "\n" + SelectMarker(2) + "Connect"
                    + "\n\n" + SelectMarker(3) + "Back";
                } else if (MenuIndex == 3)
                {
                    MenuText += "\n" + "Connecting...";
                }
                Texter.SetText(MenuText);
            }
        }

        public override void OnDestroy()
        {
            Texter.Destory();
        }
    }
}
