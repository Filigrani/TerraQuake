using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TerraQuake;
using System.Threading.Tasks;

namespace TerraQuake
{
    public class GameInstance : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        public GameTime LastRenderGameTime = new GameTime();

        public int WindowWidth = 960;
        public int WindowHeight = 540;

        public int VirtualWidth = 960;
        public int VirtualHeight = 540;
        public bool GameStarted = false;

        public void ApplyChanges()
        {
            if (_graphics != null)
            {
                _graphics.ApplyChanges();
            }
        }

        public Matrix GetScaleMatrix()
        {
            var scaleX = (float)WindowWidth / VirtualWidth;
            var scaleY = (float)WindowHeight / VirtualHeight;
            return Matrix.CreateScale(scaleX, scaleY, 1.0f);
        }

        public Point PointToScreen(Point point)
        {
            var matrix = Matrix.Invert(GetScaleMatrix());
            return Vector2.Transform(point.ToVector2(), matrix).ToPoint();
        }
        public Vector2 VectorToScreen(Vector2 V2)
        {
            var matrix = Matrix.Invert(GetScaleMatrix());
            return Vector2.Transform(V2, matrix);
        }

        public void ApplyReolustion(int W, int H, bool Apply = true)
        {
            WindowWidth = W;
            WindowHeight = H;
            _graphics.PreferredBackBufferHeight = WindowHeight;
            _graphics.PreferredBackBufferWidth = WindowWidth;
            if (Apply)
            {
                ApplyChanges();
            }
        }

        public GameInstance()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferHeight = WindowHeight;
            _graphics.PreferredBackBufferWidth = WindowWidth;
            _graphics.ApplyChanges();
            ContentManager.Game = this;
        }

        protected override void Initialize()
        {
            PrepareLevels();
            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            ContentManager.SpriteBatch = _spriteBatch;

            ContentManager.LoadSprite("Test");
            ContentManager.LoadSprite("Test2");
            ContentManager.LoadSprite("Star1");
            ContentManager.LoadSprite("Star2");
            ContentManager.LoadSprite("Star3");
            ContentManager.LoadSprite("DebugFont");
            ContentManager.LoadSprite("Green");
            ContentManager.LoadSprite("GreenTransperent");
            ContentManager.LoadSprite("DimGreen");
            ContentManager.LoadSprite("Crosshair");
            ContentManager.LoadSprite("CrosshairBig");
            ContentManager.LoadSprite("CrosshairPartly");
            ContentManager.LoadSprite("CrosshairHorizontal");
            ContentManager.LoadSprite("Balloon");
            ContentManager.LoadSprite("Lawn");
            ContentManager.LoadSprite("Ghost");
            ContentManager.LoadSprite("GhostGun");
            ContentManager.LoadSprite("GhostHands_Shotgun");
            ContentManager.LoadSprite("GhostHands_Pistol");
            ContentManager.LoadSprite("GhostHands_Nailgun");
            ContentManager.LoadSprite("GhostHands");
            ContentManager.LoadSprite("Shot");
            ContentManager.LoadSprite("TerrainTest");
            ContentManager.LoadSprite("DebugWhite");
            ContentManager.LoadSprite("SnowFlake0");
            ContentManager.LoadSprite("RainDrop");
            ContentManager.LoadSprite("MainMenuBackground");
            ContentManager.LoadSprite("TerraQuakeLogo");
            ContentManager.LoadSprite("mboxl");
            ContentManager.LoadSCML("GreyGuy", @"GreyGuy\player");
            ContentManager.LoadSCML("ghost_abigail_victorian", @"ghost_abigail_victorian\ghost_abigail_victorian");
            
        }
        SpriteFont DebugText;
        internal Ghost MyGhost = null;
        internal Terrain TerrainInstance = null;
        internal WeatherParticles Weather = null;

        internal GameObject CreateGhost()
        {
            GameObject GhostObj = GameObjectManager.CreateObject();
            Ghost GhostComp = new Ghost();
            GameObject GhostRenderObject = GameObjectManager.CreateObject();

            Renderer RenderBody = new Renderer("Player");
            Animator AnimatorBody = new Animator();
            DebugRenderer DebugRender = new DebugRenderer("Debug", new Rectangle(0, 0, (int)GhostComp.ColisionBounds.X, (int)GhostComp.ColisionBounds.Y));
            DebugRender.RenderOffset = GhostComp.ColisionOffset;
            AnimatorBody.MyRenderer = RenderBody;
            GhostComp.RendererBody = RenderBody;
            GhostComp.AnimatorBody = AnimatorBody;
            GhostRenderObject.AddComponent(DebugRender, "Debug");
            GhostRenderObject.AddComponent(RenderBody, "Body");
            GhostRenderObject.AddComponent(AnimatorBody);
            GhostObj.AddComponent(GhostComp);
            GhostObj.CanSleep = false;

            return GhostObj;
        }

        public void PrepareLevels()
        {
            LevelManager.LevelConstructor GameLevel = new LevelManager.LevelConstructor();
            GameLevel.OnLevelStart = delegate ()
            {
                LayersManager.AddLayer("BG");
                LayersManager.AddLayer("Weather").Parallax = new Vector2(30, 10);
                LayersManager.AddLayer("Objects");
                LayersManager.AddLayer("Player");
                LayersManager.AddLayer("Light");
                LayersManager.AddLayer("Debug").Visible = Settings.ChunkDebugMode != Terrain.ChunksDebug.None;
                Layer Trans = LayersManager.AddLayer("Transition");
                Trans.ScrollLock = true;

                if(DebugText == null)
                {
                    DebugText = new SpriteFont();
                    DebugText.Font = ContentManager.GetSprite("DebugFont");
                }

                GameObject Ghost = CreateGhost();
                Ghost.Position = new Vector2(VirtualWidth / 2, -100);
                MyGhost = Ghost.GetComponent(typeof(Ghost)) as Ghost;
                TerrainInstance = new Terrain();
                TerrainInstance.TerrainH = Settings.DefaultTerrainH;
                TerrainInstance.TerrainW = Settings.DefaultTerrainW;
                TerrainInstance.CreateTerrain(Settings.TerrainSeed);
                GameObject W = GameObjectManager.CreateObject();

                if(Settings.Weather != WeatherParticles.ImpactMaterial.None)
                {

                    WeatherParticles WCom = new WeatherParticles();
                    W.AddComponent(WCom);
                    Weather = WCom;
                    Weather.Terra = TerrainInstance;

                    Weather.MaterialOnImpact = Settings.Weather;

                    if (Weather.MaterialOnImpact == WeatherParticles.ImpactMaterial.Water)
                    {
                        Weather.Sprite = ContentManager.GetSprite("RainDrop");
                        Weather.ImpactSpawnRadius = 1;
                        Weather.Gravity = 2f;
                    } else
                    {
                        Weather.Sprite = ContentManager.GetSprite("SnowFlake0");
                        Weather.ImpactSpawnRadius = 2;
                        Weather.Gravity = 0.5f;
                    }
                }
                GameObject DarknessObject = GameObjectManager.CreateObject();
                Renderer DarknessRender = new Renderer("Light");
                Darkness DarknessComp = new Darkness();
                DarknessComp.MyRenderer = DarknessRender;
                DarknessObject.AddComponent(DarknessRender);
                DarknessObject.AddComponent(DarknessComp);
                DarknessComp.CreateDarkness(Color.Black, 0);
            };

            LevelManager.LevelConstructor MenuLevel = new LevelManager.LevelConstructor();
            MenuLevel.OnLevelStart = delegate ()
            {
                LayersManager.AddLayer("BG");
                LayersManager.AddLayer("MainMenu");
                Layer Trans = LayersManager.AddLayer("Transition");
                Trans.ScrollLock = true;

                GameObject BGobj = GameObjectManager.CreateObject();
                Renderer Rend = new Renderer("BG");
                Rend.SetSprite(ContentManager.GetSprite("MainMenuBackground"));
                Renderer RendLogo = new Renderer("BG");
                RendLogo.SetSprite(ContentManager.GetSprite("TerraQuakeLogo"));
                RendLogo.RenderOffset = new Vector2(VirtualWidth/2-RendLogo.Sprite.Width/2, VirtualHeight/2 - RendLogo.Sprite.Height / 2);
                BGobj.AddComponent(Rend);
                BGobj.AddComponent(RendLogo, "Logo");
                BGobj.AddComponent(new MainMenuLogic());
                if (DebugText != null)
                {
                    DebugText.Destory();
                    DebugText = null;
                }
            };

            LevelManager.LevelConstructor TestLevel = new LevelManager.LevelConstructor();
            TestLevel.OnLevelStart = delegate ()
            {
                LayersManager.AddLayer("BG");
                LayersManager.AddLayer("Main");
                Layer Trans = LayersManager.AddLayer("Transition");
                Trans.ScrollLock = true;
                //GameObject Obj = GameObjectManager.CreateObject();
                //Animator Animer = new Animator();
                //Renderer Render = new Renderer("Main");
                //Animer.MyRenderer = Render;
                //Dictionary<string, Animator.AnimationData> Dict = new Dictionary<string, Animator.AnimationData>();
                //Animator.AnimationData Test = new Animator.AnimationData("Test");
                //Test.DefaultLoop = true;
                //Test.PinPong = true;
                //Texture2D SpriteSheet = ContentManager.GetSprite("mboxl");
                //for (int i = 0; i != 9; i++)
                //{
                //    Test.AddFrame(SpriteSheet, new Rectangle(i * 149, 0, 149, 93), 100);
                //}
                //Dict.Add(Test.Name, Test);
                //Animer.Animations = Dict;
                //Animer.PlayAnimation("Test");
                //Obj.AddComponent(Animer);
                //Obj.AddComponent(Render);

                GameObject SCMLTest = GameObjectManager.CreateObject();
                SCMLTest.AddComponent(new SCMLSpriter("ghost_abigail_victorian"));
                SCMLTest.Position = new Vector2(VirtualWidth/2, VirtualHeight);
            };

            LevelManager.AddLevelConstructor("Game", GameLevel);
            LevelManager.AddLevelConstructor("Menu", MenuLevel);
            LevelManager.AddLevelConstructor("TestLevel", TestLevel);
        }

        public int PreviousScroll = 0;



        public Vector2 GetPointer()
        {
            Vector2 Position = new Vector2(Mouse.GetState().Position.X - LayersManager.Scrolling.X, Mouse.GetState().Position.Y - LayersManager.Scrolling.Y) / LayersManager.Scaler;
            int PositionX = (int)Position.X;
            int PositionY = (int)Position.Y;
            return new Vector2(PositionX, PositionY);
        }
        protected override void Update(GameTime gameTime)
        {
            if (!GameStarted)
            {
                LevelManager.StartLevel("Menu", false);
                GameStarted = true;
            }
            //IsMouseVisible = false;
            if (TerrainInstance != null)
            {
                TerrainInstance.Update();
            }

            if (IsActive)
            {
                Input.Update();

                if (Input.KeyPressed(Keys.Escape))
                {
                    if (LevelManager.CurrentLevel == "Menu")
                    {
                        Exit();
                    } else
                    {
                        LevelManager.StartLevel("Menu");
                    }
                }

                if (Input.KeyPressed(Keys.F11) || Input.KeyPressed(Keys.F))
                {
                    if (_graphics.IsFullScreen == true)
                    {
                        ApplyReolustion(960, 540, false);
                        _graphics.IsFullScreen = false;
                        LayersManager.Scaler = 1;
                        ApplyChanges();
                    } else
                    {
                        ApplyReolustion(1920, 1080, false);
                        //_graphics.IsFullScreen = true;
                        LayersManager.Scaler = 2;
                        ApplyChanges();
                    }
                }
                if (Input.KeyPressed(Keys.F5))
                {
                    if (LevelManager.CurrentLevel == "Game")
                    {
                        LevelManager.StartLevel("Game");
                    }
                }
                if (Input.KeyPressed(Keys.M))
                {
                    Network.Test();
                }

                bool Click = Mouse.GetState().LeftButton == ButtonState.Pressed;
                bool Click2 = Mouse.GetState().RightButton == ButtonState.Pressed;

                if (Keyboard.GetState().IsKeyDown(Keys.Right))
                {
                    foreach (Animator Animator in Animator.Animators)
                    {
                        if (Animator.ByFrameDebug)
                        {
                            if (Animator.CurrentAnimation != null)
                            {
                                Animator.ByPassDebug = true;
                            }
                        }
                    }
                }
                if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                {
                    if (TerrainInstance != null && TerrainInstance.ManualUpdate)
                    {
                        if (!TerrainInstance.UpdateThreadsCreated || (TerrainInstance.UpdateThreadsCreated && (TerrainInstance.UpdateTerrainThread == null || TerrainInstance.UpdateTerrainThread.IsCompleted)))
                        {
                            if (TerrainInstance.UpdateTerrainThread != null)
                            {
                                TerrainInstance.UpdateTerrainThread.Dispose();
                                TerrainInstance.UpdateTerrainThread = null;
                            }
                            TerrainInstance.UpdateThreadsCreated = true;
                            TerrainInstance.UpdateTerrainThread = Task.Factory.StartNew(TerrainInstance.ProcessAllChunks);
                        }
                    }
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    if (MyGhost != null)
                    {
                        MyGhost.JumpForward();
                    }
                }
            }

            GameObjectManager.Update(gameTime);
            WaveManager.Update(gameTime);
            base.Update(gameTime);
        }

        public void ForceDraw()
        {
            Draw(LastRenderGameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            LastRenderGameTime = gameTime;
            GraphicsDevice.Clear(Color.SkyBlue);
            GameObjectManager.Render(gameTime);
            LayersManager.Render(gameTime);
            PacketHandler.Update();
            string Text = "";
            if (DebugText != null)
            {
                //if (TerrainInstance != null && MyGhost != null)
                //{
                //    Text = "Colide " + TerrainInstance.CheckCollision(MyGhost.GetPhysicalColision2())
                //        + "\nG " + MyGhost.OnGround + " L " + MyGhost.LeftBlocked + " R " + MyGhost.RightBlocked
                //        + "\nLast Scan X " + TerrainInstance.LastScanX + " " + TerrainInstance.LastScanXEnd
                //        + "\nLast Scan Y " + TerrainInstance.LastScanY + " " + TerrainInstance.LastScanYEnd
                //        + "\nLongest Update " + TerrainInstance.LongetsUpdateMs + "ms"
                //        + "\nChunks " + TerrainInstance.Chunks.Count + " ChunkRow " + TerrainInstance.ChunksRow
                //        + "\nLast Hole X " + TerrainInstance.LastHole.X + " Y " + TerrainInstance.LastHole.Y
                //        + "\nPos X " + MyGhost.Object.Position.X + " Y " + MyGhost.Object.Position.Y;
                //}
                if (TerrainInstance != null && MyGhost != null)
                {
                    Text = "Terrain Age " + TerrainInstance.CurrentAge
                        + "\nDirection Index " + TerrainInstance.LastRandomDirectionIndex
                        + "\nRolling Index " + TerrainInstance.LastRandomRollingNumsIndex
                        + "\nLast Hole X " + TerrainInstance.LastHole.X + " Y " + TerrainInstance.LastHole.Y;
                }
                //if (TerrainInstance != null && MyGhost != null)
                //{
                //    Text = "T - Make Hole"
                //        + "\nY - Ball of snow"
                //        + "\nU - Water"
                //        + "\nB Benchmark holes"
                //        + "\nR - New terrain"
                //        + "\nK - Teleport"
                //        + "\nF5 - Restart level"
                //        + "\nEsc - Back to menu";
                //}
                DebugText.SetText(Text);
            }
            foreach (SpriteFont Font in SpriteFont.SpriteFonts)
            {
                Font.Render(_spriteBatch);
            }

            if(TerrainInstance != null)
            {
                TerrainInstance.Draw(gameTime, _graphics.GraphicsDevice);
            }

            base.Draw(gameTime);
        }
    }
}