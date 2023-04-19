using Microsoft.Xna.Framework;
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


        public class LevelConstructor
        {
            public string Name = "";
            public Action OnLevelStart = null;
        }
        
        
        public static void StartLevel(string LevelName)
        {
            LevelConstructor Lvl = GetLevelConstructor(LevelName);

            if(Lvl == null)
            {
                return;
            }
            UnloadLevel();

            if(Lvl.OnLevelStart != null)
            {
                Lvl.OnLevelStart();
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
