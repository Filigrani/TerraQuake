using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class LayersManager
    {
        public static List<Layer> Layers = new List<Layer>();
        public static Dictionary<string, int> LayersIndexes = new Dictionary<string, int>();
        public static Vector2 Scrolling = new Vector2(0, 0);
        public static Vector2 DesiredScrolling = new Vector2(0, 0);
        public static Vector2 LastScrollPosition = new Vector2(0, 0);
        public static bool SmoothScrolling = false;
        public static float SmoothnessAmout = 8;
        public static float Scaler = 1;
        public static Vector2 ScrollOffset = new Vector2(0, 0);

        public static void Wipe(bool DeleteLayers = false)
        {
            foreach (Layer L in Layers)
            {
                L.DebugRenderers.Clear();
                L.Renderers.Clear();
            }

            if (DeleteLayers)
            {
                Layers.Clear();
                LayersIndexes.Clear();
            }
        }

        public static void Reset()
        {
            DesiredScrolling = new Vector2(0, 0);
            Scrolling = new Vector2(0, 0);
        }


        public static Layer AddLayer(string Name)
        {
            Layer NewLayer = new Layer(Name);
            Layers.Add(NewLayer);
            LayersIndexes.Add(Name, Layers.Count-1);
            return NewLayer;
        }
        public static Layer GetLayer(string Name)
        {
            int Idx = GetLayerIndex(Name);
            return GetLayer(Idx);
        }
        public static Layer GetLayer(int Index)
        {
            return Layers[Index];
        }
        public static int GetLayerIndex(string Name)
        {
            int Idx;
            if(LayersIndexes.TryGetValue(Name, out Idx))
            {
                return Idx;
            }
            return -1;
        }

        public static void MoveToLayer(Renderer Renderer, string DesiredLayerName)
        {
            Layer CurLayer = Renderer.Layer;
            Layer DesiredLayer = GetLayer(GetLayerIndex(DesiredLayerName));
            CurLayer.RemoveRenderer(Renderer);
            DesiredLayer.AddRenderer(Renderer);
            Renderer.Layer = DesiredLayer;
        }

        public static void ScrollTo(Vector2 ScrollPosition)
        {
            LastScrollPosition = ScrollPosition;
            Vector2 OnScreenPosition = new Vector2(ContentManager.Game.SceneWidth / 2, ContentManager.Game.SceneHeight / 2);
            Vector2 ScrollTo = new Vector2(OnScreenPosition.X + -ScrollPosition.X, OnScreenPosition.Y + -ScrollPosition.Y) * Scaler + ScrollOffset;

            if (!SmoothScrolling)
            {
                Scrolling = ScrollTo;
            }else{
                DesiredScrolling = ScrollTo;
            }
        }
        public static void ScrollTo(Vector2 ScrollPosition, Vector2 OnScreenPosition)
        {
            Vector2 ScrollTo = new Vector2(OnScreenPosition.X + -ScrollPosition.X, OnScreenPosition.Y + -ScrollPosition.Y);
            if (!SmoothScrolling)
            {
                Scrolling = ScrollTo;
            }else{
                DesiredScrolling = ScrollTo;
            }
        }
        public static void Render(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (SmoothScrolling)
            {
                Scrolling = Vector2.Lerp(Scrolling, DesiredScrolling, deltaTime * SmoothnessAmout);
            }

            foreach (Layer item in Layers)
            {
                item.SetScroll(Scrolling);
                item.Render();
            }
        }
    }

    internal class Layer
    {
        public string Name = "";
        public List<Renderer> Renderers = new();
        public List<DebugRenderer> DebugRenderers = new();
        public float Depth = 0;
        public Vector2 Position = new(0, 0);
        public Vector2 Parallax = new(1, 1);
        public bool ScrollLock = false;
        public bool Visible = true;
        public Layer(string _Name)
        {
            Name = _Name;
        }
        public void AddRenderer(Renderer Renderer)
        {
            Renderers.Add(Renderer);
        }
        public void RemoveRenderer(Renderer Renderer)
        {
            Renderers.Remove(Renderer);
        }
        public void AddRenderer(DebugRenderer Renderer)
        {
            DebugRenderers.Add(Renderer);
        }
        public void RemoveRenderer(DebugRenderer Renderer)
        {
            DebugRenderers.Remove(Renderer);
        }
        public void Render()
        {
            if (!Visible)
            {
                return;
            }
            foreach (Renderer Renderer in Renderers)
            {
                Renderer.Render();
            }
            foreach (DebugRenderer Renderer in DebugRenderers)
            {
                Renderer.Render();
            }
        }
        public void SetParallax(Vector2 Pllx)
        {
            Parallax = Pllx;
        }
        public void SetParallax(float _x, float _y)
        {
            Parallax = new Vector2(_x, _y);
        }
        public void SetScroll(Vector2 Scroll)
        {
            if (!ScrollLock)
            {
                Position = Scroll * Parallax;
            }
        }
    }
}
