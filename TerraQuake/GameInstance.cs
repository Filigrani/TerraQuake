using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
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

        public int SceneWidth = 960;
        public int SceneHeight = 540;
        public bool GameStarted = false;

        public void ApplyReolustion(int W, int H, bool Apply = true)
        {
            WindowWidth = W;
            WindowHeight = H;
            _graphics.PreferredBackBufferHeight = WindowHeight;
            _graphics.PreferredBackBufferWidth = WindowWidth;
            if (Apply)
            {
                _graphics.ApplyChanges();
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
        }
        SpriteFont DebugText;
        internal Ghost MyGhost = null;
        internal Terrain TerrainInstance = null;
        internal WeatherParticles Weather = null;

        internal GameObject CreateGhost()
        {
            GameObject GhostObj = GameObjectManager.CreateObject();
            Ghost GhostComp = new Ghost();

            Renderer RenderBody = new Renderer("Player");
            Renderer RenderGun = new Renderer("Player");
            Renderer RenderHands = new Renderer("Player");
            Animator AnimatorBody = new Animator();
            Animator AnimatorHands = new Animator();
            Animator AnimatorGun = new Animator();
            DebugRenderer DebugRender = new DebugRenderer("Debug", new Rectangle(0, 0, (int)GhostComp.ColisionBounds.X, (int)GhostComp.ColisionBounds.Y));
            DebugRender.RenderOffset = GhostComp.ColisionOffset;
            AnimatorBody.MyRenderer = RenderBody;
            AnimatorHands.MyRenderer = RenderHands;
            AnimatorGun.MyRenderer = RenderGun;
            GhostComp.RendererBody = RenderBody;
            GhostComp.AnimatorBody = AnimatorBody;

            RenderHands.RenderOffset = new Vector2(5, 9);
            RenderGun.RenderOffset = new Vector2(51, 10);

            GhostObj.AddComponent(AnimatorBody, "AnimatorBody");
            GhostObj.AddComponent(AnimatorHands, "AnimatorHands");
            GhostObj.AddComponent(AnimatorGun, "AnimatorGun");
            GhostObj.AddComponent(RenderBody, "RenderBody");
            GhostObj.AddComponent(RenderHands, "RenderHands");
            GhostObj.AddComponent(RenderGun, "RenderGun");
            GhostObj.AddComponent(DebugRender);
            GhostObj.AddComponent(GhostComp);
            GhostObj.CanSleep = false;

            return GhostObj;
        }

        public void PrepareLevels()
        {
            bool Debug = false;
            LevelManager.LevelConstructor GameLevel = new LevelManager.LevelConstructor();
            GameLevel.OnLevelStart = delegate ()
            {
                LayersManager.AddLayer("BG");
                LayersManager.AddLayer("Weather").Parallax = new Vector2(30, 10);
                LayersManager.AddLayer("Objects");
                LayersManager.AddLayer("Player");
                LayersManager.AddLayer("Debug").Visible = Debug;
                Layer Trans = LayersManager.AddLayer("Transition");
                Trans.ScrollLock = true;

                GameObject Ghost = CreateGhost();
                Ghost.Position = new Vector2(SceneWidth / 2, -100);
                MyGhost = Ghost.GetComponent(typeof(Ghost)) as Ghost;

                //if (DebugText == null)
                //{
                //    DebugText = new SpriteFont();
                //    DebugText.Font = ContentManager.GetSprite("DebugFont");
                //}
                TerrainInstance = new Terrain();
                TerrainInstance.CreateTerrain(228);

                GameObject W = GameObjectManager.CreateObject();
                WeatherParticles WCom = new WeatherParticles();
                W.AddComponent(WCom);
                Weather = WCom;
                Weather.Terra = TerrainInstance;

                Weather.MaterialOnImpact = WeatherParticles.ImpactMaterial.Water;

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
                BGobj.AddComponent(Rend);
            };

            LevelManager.AddLevelConstructor("Game", GameLevel);
            LevelManager.AddLevelConstructor("Menu", MenuLevel);
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (IsActive)
            {
                Input.Update();

                if(Input.KeyPressed(Keys.F11) || Input.KeyPressed(Keys.F))
                {
                    if (_graphics.IsFullScreen == true)
                    {
                        ApplyReolustion(960, 540, false);
                        _graphics.IsFullScreen = false;
                        LayersManager.Scaler = 1;
                        _graphics.ApplyChanges();
                    } else
                    {
                        ApplyReolustion(1920, 1080, false);
                        _graphics.IsFullScreen = true;
                        LayersManager.Scaler = 2;
                        _graphics.ApplyChanges();
                    }
                }

                if (Input.KeyPressed(Keys.Enter))
                {
                    if(LevelManager.CurrentLevel == "Menu")
                    {
                        LevelManager.StartLevel("Game");
                    }
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
                        MyGhost.Jump();
                    }
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Enter))
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
            string Text = "";
            if (DebugText != null)
            {
                if (TerrainInstance != null && MyGhost != null)
                {
                    Text = "Colide " + TerrainInstance.CheckCollision(MyGhost.GetPhysicalColision2())
                        + "\nG " + MyGhost.OnGround + " L " + MyGhost.LeftBlocked + " R " + MyGhost.RightBlocked
                        + "\nLast Scan X " + TerrainInstance.LastScanX + " " + TerrainInstance.LastScanXEnd
                        + "\nLast Scan Y " + TerrainInstance.LastScanY + " " + TerrainInstance.LastScanYEnd
                        + "\nLongest Update " + TerrainInstance.LongetsUpdateMs + "ms"
                        + "\nChunks " + TerrainInstance.Chunks.Count + " ChunkRow " + TerrainInstance.ChunksRow
                        + "\nLast Hole X " + TerrainInstance.LastHole.X + " Y " + TerrainInstance.LastHole.Y
                        + "\nPos X " + MyGhost.Object.Position.X + " Y " + MyGhost.Object.Position.Y;
                }
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