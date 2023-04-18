using TerraQuake;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class WeatherParticles : Component
    {
        public List<Renderer> Renderers = new List<Renderer>();
        public Texture2D Sprite = ContentManager.GetSprite("SnowFlake0");
        public float Wind = 0.3f;
        public float TargetWind = 0.3f;
        public float Gravity = 0.5f;
        public Random WeatherRandom = new Random(345435);
        public TimeSpan LastSpawn = TimeSpan.Zero;
        public TimeSpan NextWindChange = TimeSpan.Zero;
        public Terrain Terra = null;
        public int MilisecondsSpawnRate = 100;


        public override void Update(GameTime gameTime)
        {
            if(LastSpawn + TimeSpan.FromMilliseconds(MilisecondsSpawnRate) < gameTime.TotalGameTime)
            {
                LastSpawn = gameTime.TotalGameTime;
                Spawn();
            }

            if(NextWindChange == TimeSpan.Zero || NextWindChange < gameTime.TotalGameTime)
            {
                NextWindChange = gameTime.TotalGameTime + TimeSpan.FromSeconds(WeatherRandom.Next(1, 10));
                TargetWind = WeatherRandom.Next(-1, 1) + WeatherRandom.NextSingle();
                MilisecondsSpawnRate = WeatherRandom.Next(10, 100);
            }
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Wind = Wind * (1 - delta) + TargetWind * delta;


            for (int i = Renderers.Count-1; i != -1; i--)
            {
                Renderer Particle = Renderers[i];

                Particle.RenderOffset.X += Wind + WeatherRandom.NextSingle();
                Particle.RenderOffset.Y += Gravity;

                if (Particle.RenderOffset.Y > 5)
                {
                    if (Terra != null)
                    {
                        if (Terra.CheckCollision(new Rectangle((int)Particle.RenderOffset.X, (int)(Particle.RenderOffset.Y+Gravity), 4, 4), false))
                        {
                            Terra.MakeSnow((int)Particle.RenderOffset.X, (int)Particle.RenderOffset.Y, 2);
                            Object.RemoveComponent(typeof(Renderer), Particle.name);
                            Renderers.RemoveAt(i);
                            Particle.Layer.RemoveRenderer(Particle);
                        }
                    }
                }
            }
        }

        public void Spawn()
        {
            Renderer Particle = new Renderer("BG");
            Particle.SetSprite(Sprite);
            Renderers.Add(Particle);

            if(Terra != null)
            {
                Particle.RenderOffset.X = WeatherRandom.Next(-100, Terra.TerrainW+100);
                Particle.RenderOffset.Y = WeatherRandom.Next(-300, -200);
            }
            Object.AddComponent(Particle, Guid.NewGuid().ToString());
        }
    }
}
