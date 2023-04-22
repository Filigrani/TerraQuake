using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class LevelManager
    {
        public static Dictionary<string, LevelConstructor> Levels = new Dictionary<string, LevelConstructor>();
        public static string CurrentLevel = "Boot";
        public static Texture2D LastFrame = null;
        public static GameObject MeltObject = null;

        public class LevelConstructor
        {
            public string Name = "";
            public Action OnLevelStart = null;
        }

        public static void CaptureLastFrame()
        {   
            GraphicsDevice Device = ContentManager.Game.GraphicsDevice;
            int w = Device.PresentationParameters.BackBufferWidth;
            int h = Device.PresentationParameters.BackBufferHeight;
            int[] backBuffer = new int[w * h];
            ContentManager.Game.ForceDraw();
            Device.GetBackBufferData(backBuffer);
            Texture2D texture = new Texture2D(Device, w, h, false, Device.PresentationParameters.BackBufferFormat);
            texture.SetData(backBuffer);
            LastFrame = texture;
        }

        public static void DoTransitionScreen()
        {
            if(LastFrame == null)
            {
                return;
            }

            if(MeltObject != null)
            {
                MeltObject.Destroy();
            }

            MeltObject = GameObjectManager.CreateObject(); 
            Renderer MeltObjectRender = new Renderer(LayersManager.Layers[LayersManager.Layers.Count-1].Name);
            MeltRenderer Melter = new MeltRenderer(MeltObjectRender);
            MeltObjectRender.SetSprite(LastFrame);
            MeltObject.AddComponent(MeltObjectRender);
            MeltObject.AddComponent(Melter);
            Melter.StartMelt();
            LastFrame = null;
        }
        
        
        public static void StartLevel(string LevelName, bool TransitionEffect = true)
        {
            LevelConstructor Lvl = GetLevelConstructor(LevelName);

            if(Lvl == null)
            {
                return;
            }


            if (TransitionEffect)
            {
                CaptureLastFrame();
            }
            
            UnloadLevel();

            if(Lvl.OnLevelStart != null)
            {
                Lvl.OnLevelStart();
            }

            if (TransitionEffect)
            {
                DoTransitionScreen();
            }
            CurrentLevel = Lvl.Name;
        }

        public static LevelConstructor GetLevelConstructor(string LevelName)
        {
            LevelConstructor Lvl;
            if(Levels.TryGetValue(LevelName, out Lvl))
            {
                return Lvl;
            } else
            {
                return null;
            }
        }
        public static void AddLevelConstructor(string LevelName, LevelConstructor Lvl)
        {
            Lvl.Name = LevelName;
            Levels.Remove(LevelName);
            Levels.Add(LevelName, Lvl);
        }
        public static void UnloadLevel()
        {
            GameObjectManager.Wipe();
            LayersManager.Wipe(true);
            LayersManager.Reset();
        }
    }
}
