using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    public class ContentManager
    {
        public static Dictionary<string, Texture2D> Sprites = new Dictionary<string, Texture2D>();
        public static Dictionary<string, string> SCMLs = new Dictionary<string, string>();
        public static GameInstance Game;
        public static SpriteBatch SpriteBatch;

        public static Texture2D LoadSprite(string Name)
        {
            if (!Sprites.ContainsKey(Name))
            {
                Texture2D Sprite = Game.Content.Load<Texture2D>(Name);
                Sprites.Add(Name, Sprite);
                return Sprite;
            }
            return null;
        }
        public static void AddSprite(string Name, Texture2D Sprite)
        {
            if (!Sprites.ContainsKey(Name))
            {
                Sprites.Add(Name, Sprite);
            }
        }
        public static Texture2D GetSprite(string Name, bool tryLoadIfNotFound = false)
        {
            Texture2D Sprite;
            if (Sprites.TryGetValue(Name, out Sprite))
            {
                return Sprite;
            }
            if (tryLoadIfNotFound)
            {
                return LoadSprite(Name);
            }
            return null;
        }

        public static string LoadSCML(string Name, string _Path)
        {
            if (!SCMLs.ContainsKey(Name))
            {
                string Path = Environment.CurrentDirectory + @"\Content\" + _Path + ".scml";
                //if (File.Exists(Path))
                //{
                    string Data = File.ReadAllText(Path);
                    if (!string.IsNullOrEmpty(Data))
                    {
                        SCMLs.Add(Name, Data);
                        return Data;
                    }
                //}
            }
            return null;
        }
        public static string GetSCMLData(string Name)
        {
            string Data;
            if (SCMLs.TryGetValue(Name, out Data))
            {
                return Data;
            }
            return null;
        }
    }
}
