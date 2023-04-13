using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TerraQuake;

namespace BalloonInvasion
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public int WindowWidth = 960;
        public int WindowHeight = 540;

        public int SceneWidth = 960;
        public int SceneHeight = 540;

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

        public Game1()
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
            LayersManager.AddLayer("BG");
            LayersManager.AddLayer("Objects");
            LayersManager.AddLayer("Player");

            bool Debug = true;

            LayersManager.AddLayer("Debug").Visible = Debug;

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
        }
        SpriteFont DebugText;
        internal Ghost MyGhost = null;
        public bool ClickPressed = false;
        public KeyboardState PreviousKeyboardState;
        internal Terrain TerrainInstance = null;

        internal GameObject CreateBalloon()
        {
            GameObject BalloonObj = GameObjectManager.CreateObject();
            Balloon BallonComp = new Balloon();

            Renderer Render = new Renderer("Objects");
            DebugRenderer DebugRender = new DebugRenderer("Debug", new Rectangle(0, 0, (int)BallonComp.ColisionBounds.X, (int)BallonComp.ColisionBounds.Y));
            DebugRender.RenderOffset = BallonComp.ColisionOffset;
            Animator Animator = new Animator();
            Animator.MyRenderer = Render;
            BallonComp.MyAnimator = Animator;
            BallonComp.MyRenderer = Render;
            BalloonObj.AddComponent(Animator);
            BalloonObj.AddComponent(Render);
            BalloonObj.AddComponent(DebugRender);
            BalloonObj.AddComponent(BallonComp);
            BalloonObj.CanSleep = false;

            Renderer BgRender = new Renderer("Objects");
            Renderer FillRender = new Renderer("Objects");
            BgRender.SetSprite(ContentManager.GetSprite("DimGreen"), new Rectangle(0, 0, 1, 1));
            FillRender.SetSprite(ContentManager.GetSprite("Green"), new Rectangle(0, 0, 1, 1));
            ProgressBar Bar = new ProgressBar();
            Bar.BgRenderer = BgRender;
            Bar.FillRenderer = FillRender;
            Bar.BarSize = new Rectangle(0, 0, 30, 5);
            Bar.Offset.X = Bar.BarSize.Height*2;
            BallonComp.HealthBar = Bar;
            BalloonObj.AddComponent(BgRender, "BgRender");
            BalloonObj.AddComponent(FillRender, "FillRender");
            BalloonObj.AddComponent(Bar);

            Random RNG = new Random();
            BalloonObj.Position.X = RNG.Next(46, 754);
            BalloonObj.Position.Y = 500;

            return BalloonObj;
        }

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

        public bool LevelStarted = false;

        public void OnLevelStarted(GameTime gameTime)
        {
            GameObject Ghost = CreateGhost();
            Ghost.Position = new Vector2(SceneWidth / 2, -100);
            MyGhost = Ghost.GetComponent(typeof(Ghost)) as Ghost;
            //GameObject BG = GameObjectManager.CreateObject();
            //Renderer BGRender = new Renderer("BG");
            //BGRender.SetSprite(ContentManager.GetSprite("Lawn"));
            //BG.AddComponent(BGRender);

            if (DebugText == null)
            {
                DebugText = new SpriteFont();
                DebugText.Font = ContentManager.GetSprite("DebugFont");
            }
            TerrainInstance = new Terrain();
            TerrainInstance.CreateTerrain();
        }

        public int PreviousScroll = 0;

        public void CheckButtonPressed(GameTime gameTime)
        {
            KeyboardState CurrentKeys = Keyboard.GetState();
            Keys[] Keys = PreviousKeyboardState.GetPressedKeys();
            foreach (Keys Key in Keys)
            {
                if (CurrentKeys.IsKeyUp(Key))
                {
                    KeyPress(Key, gameTime);
                }
            }
        }

        public Vector2 GetPointer()
        {
            Vector2 Position = new Vector2(Mouse.GetState().Position.X - LayersManager.Scrolling.X, Mouse.GetState().Position.Y - LayersManager.Scrolling.Y) / LayersManager.Scaler;
            int PositionX = (int)Position.X;
            int PositionY = (int)Position.Y;
            return new Vector2(PositionX, PositionY);
        }

        public void KeyPress(Keys Key, GameTime gameTime)
        {
            if(Key == Keys.Down)
            {
                foreach (Animator Animator in Animator.Animators)
                {
                    if (Animator.ByFrameDebug)
                    {
                        if (Animator.CurrentAnimation != null)
                        {
                            Animator.CurrentAnimation.DoPreviousFrame(gameTime);
                        }
                    }
                }
            } else if(Key == Keys.Up)
            {
                foreach (Animator Animator in Animator.Animators)
                {
                    if (Animator.ByFrameDebug)
                    {
                        if(Animator.CurrentAnimation != null)
                        {
                            Animator.CurrentAnimation.DoNextFrame(gameTime);
                        }
                    }
                }
            }else if(Key == Keys.F11 || Key == Keys.F)
            {
                if(_graphics.IsFullScreen == true)
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
            }else if(Key == Keys.T)
            {
                if(TerrainInstance != null)
                {
                    int X = (int)GetPointer().X;
                    int Y = (int)GetPointer().Y;

                    TerrainInstance.MakeHole(X, Y, 40);
                }
            }else if(Key == Keys.B)
            {
                TerrainInstance.LongetsUpdateMs = 0;
                TerrainInstance.BenchHoles();
            } else if (Key == Keys.R)
            {
                TerrainInstance.CreateTerrain();
            } else if (Key == Keys.Y)
            {
                if (TerrainInstance != null)
                {
                    int X = (int)GetPointer().X;
                    int Y = (int)GetPointer().Y;

                    TerrainInstance.MakeDirt(X, Y, 10);
                }
            } else if (Key == Keys.LeftShift)
            {
                if (TerrainInstance != null && TerrainInstance.ManualUpdate)
                {
                    TerrainInstance.FallingPixels();
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            //IsMouseVisible = false;
            if (TerrainInstance != null)
            {
                TerrainInstance.Update();
            }
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (!LevelStarted)
            {
                LevelStarted = true;
                OnLevelStarted(gameTime);
            }

            if (IsActive)
            {
                CheckButtonPressed(gameTime);

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
                    //if (TerrainInstance != null && TerrainInstance.ManualUpdate)
                    //{
                    //    TerrainInstance.FallingPixels();
                    //}
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

            PreviousKeyboardState = Keyboard.GetState();
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Orange);

            LayersManager.Render(gameTime);
            int Popped = (int)(0.5f + ((100f * Balloon.PopedBalloons) / Balloon.TotalBalloons));
            int Missed = (int)(0.5f + ((100f * Balloon.MissedBalloons) / Balloon.TotalBalloons));

            string Text = "Colide " + TerrainInstance.CheckCollision(MyGhost.GetPhysicalColision2())
                + "\nG " + MyGhost.OnGround + " L " + MyGhost.LeftBlocked + " R " + MyGhost.RightBlocked
                + "\nLast Scan X " + TerrainInstance.LastScanX + " " + TerrainInstance.LastScanXEnd
                + "\nLast Scan Y " + TerrainInstance.LastScanY + " " + TerrainInstance.LastScanYEnd
                + "\nLongest Update " + TerrainInstance.LongetsUpdateMs + "ms"
                + "\nLast Hole X " + TerrainInstance.LastHole.X + " Y " + TerrainInstance.LastHole.Y
                + "\nPos X " + MyGhost.Object.Position.X + " Y " + MyGhost.Object.Position.Y;
            DebugText.SetText(Text);
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