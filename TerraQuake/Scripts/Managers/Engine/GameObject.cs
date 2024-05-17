using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TerraQuake
{
    internal class GameObject
    {
        public Vector2 Position = new Vector2(0, 0);
        public string GUID = "";
        public Dictionary<Type, Dictionary<string, Component>> Components = new Dictionary<Type, Dictionary<string, Component>>();
        public Dictionary<string, bool> Tags = new Dictionary<string, bool>();
        public bool CanSleep = false;
        public Component AddComponent(Component comp, string Name = "")
        {
            Type T = comp.GetType();
            Dictionary<string, Component> Comps;
            if(!Components.TryGetValue(T, out Comps))
            {
                Comps = new Dictionary<string, Component>();
            }

            if(!Comps.ContainsKey(Name))
            {
                comp.Object = this;
                comp.name = Name;
                Comps.Add(Name, comp);
                Components.Remove(T);
                Components.Add(T, Comps);
                comp.OnAttached();
            }


            return comp;
        }
        public Component GetComponent(Type T, string Name = "")
        {
            Dictionary<string, Component> Comps;
            if(Components.TryGetValue(T, out Comps))
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return Comps.First().Value;
                } else
                {
                    Component Comp;
                    if(Comps.TryGetValue(Name, out Comp))
                    {
                        return Comp;
                    }
                }
            }

            return null;
        }
        public void RemoveComponent(Type T, string Name = "")
        {
            Dictionary<string, Component> Comps;
            if (Components.TryGetValue(T, out Comps))
            {
                string Key = Name;

                if (string.IsNullOrEmpty(Key))
                {
                    Key = Comps.Last().Key;
                }

                Comps.Remove(Key);
                Components.Remove(T);
                Components.Add(T, Comps);
            }
        }

        public void AddTag(string Name)
        {
            if (!Tags.ContainsKey(Name))
            {
                Tags.Add(Name, true);
            }
        }
        public void RemoveTag(string Name)
        {
            Tags.Remove(Name);
        }
        public bool HasTag(string Name)
        {
            return Tags.ContainsKey(Name);
        }

        public void OnDestroy()
        {
            foreach (var item in Components)
            {
                Dictionary<string, Component> Comps = item.Value;
                foreach (var item2 in Comps)
                {
                    item2.Value.OnDestroy();
                }
            }
        }

        public void Destroy()
        {
            GameObjectManager.DestroyObject(GUID);
        }

        public bool IsSleeping()
        {
            if (!CanSleep)
            {
                return false;
            }
            
            
            return Vector2.Distance(GameObjectManager.PerspectivePosition, Position) > GameObjectManager.UpdateDistance;
        }

        public void Update(GameTime gameTime)
        {
            if (IsSleeping())
            {
                return;
            }
            
            foreach (var item in Components.ToList())
            {
                Dictionary<string, Component> Comps = item.Value;
                foreach (var item2 in Comps)
                {
                    item2.Value.Update(gameTime);
                }
            }
        }
        public void Render(GameTime gameTime)
        {
            if (IsSleeping())
            {
                return;
            }

            foreach (var item in Components.ToList())
            {
                Dictionary<string, Component> Comps = item.Value;
                foreach (var item2 in Comps)
                {
                    item2.Value.Render(gameTime);
                }
            }
        }
        public void OnAnimationFinished(GameTime gameTime, string AnimationName, string SenderName) 
        {
            foreach (var item in Components)
            {
                Dictionary<string, Component> Comps = item.Value;
                foreach (var item2 in Comps)
                {
                    item2.Value.OnAnimationFinished(gameTime, AnimationName, SenderName);
                }
            }
        }
    }
}
