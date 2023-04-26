using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TerraQuake
{
    internal class GameObjectManager
    {
        public static Dictionary<string, GameObject> Objects = new Dictionary<string, GameObject>();
        public static Vector2 PerspectivePosition = new Vector2(0, 0);
        public static float UpdateDistance = 700;

        public static void Wipe()
        {
            List<KeyValuePair<string, GameObject>> Li = Objects.ToList();
            for (int i = Li.Count-1; i != -1; i--)
            {
                Li[i].Value.Destroy();
            }
        }

        public static GameObject CreateObject()
        {
            return CreateObject(new Vector2(0,0), "");
        }
        public static GameObject CreateObject(Vector2 v2, string GUID = "")
        {
            GameObject NewObj = new GameObject();
            NewObj.Position = v2;

            if (string.IsNullOrEmpty(GUID))
            {
                GUID = Guid.NewGuid().ToString();
                NewObj.GUID = GUID;
            }

            Objects.Add(GUID, NewObj);

            return NewObj;
        }
        public static GameObject GetObject(string GUID)
        {
            GameObject obj;
            if(Objects.TryGetValue(GUID, out obj))
            {
                return obj;
            }
            return null;
        }
        public static void DestroyObject(string GUID)
        {
            GameObject obj;
            if (Objects.TryGetValue(GUID, out obj))
            {
                obj.OnDestroy();
                Objects.Remove(GUID);
            }
        }
        public static void Update(GameTime gameTime)
        {
            foreach (var item in Objects.ToList())
            {
                item.Value.Update(gameTime);
            }
        }
        public static void Render(GameTime gameTime)
        {
            foreach (var item in Objects)
            {
                item.Value.Render(gameTime);
            }
        }
    }
}
