using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    public class ContentManager
    {
        public static Dictionary<string, Texture2D> Sprites = new Dictionary<string, Texture2D>();
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
        public static Texture2D GetSprite(string Name)
        {
            Texture2D Sprite;
            if (Sprites.TryGetValue(Name, out Sprite))
            {
                return Sprite;
            }
            return null;
        }
    }
}
