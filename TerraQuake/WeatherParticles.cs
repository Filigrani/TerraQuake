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
        public Texture2D Sprite = null;
        public float Wind = 0.3f;
        public float TargetWind = 0.3f;
        public float Gravity = 0.5f;
        public Random WeatherRandom = new Random(345435);
        public TimeSpan NextSpawn = TimeSpan.Zero;
        public TimeSpan NextWindChange = TimeSpan.Zero;
        public Terrain Terra = null;
        public int SpawnRateMin = 10;
        public int SpawnRateMax = 100;
        public int WindChangeMin = 10;
        public int WindChangeMax = 100;
        public int WindPower = 1;
        public int ImpactSpawnRadius = 2;
        public ImpactMaterial MaterialOnImpact = ImpactMaterial.None;
        public enum ImpactMaterial
        {
            None = 0,
            Snow,
            Water,
        }

        public override void Update(GameTime gameTime)
        {
            if(Sprite == null)
            {
                return;
            }
            if(NextSpawn == TimeSpan.Zero || NextSpawn < gameTime.TotalGameTime)
            {
                NextSpawn = gameTime.TotalGameTime + TimeSpan.FromMilliseconds(WeatherRandom.Next(SpawnRateMin, SpawnRateMax));
                Spawn();
            }

            if(NextWindChange == TimeSpan.Zero || NextWindChange < gameTime.TotalGameTime)
            {
                NextWindChange = gameTime.TotalGameTime + TimeSpan.FromMilliseconds(WeatherRandom.Next(WindChangeMin, WindChangeMax));
                TargetWind = WeatherRandom.Next(-WindPower, WindPower) + WeatherRandom.NextSingle();
            }
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Wind = Wind * (1 - delta) + TargetWind * delta;
            for (int i = Renderers.Count-1; i != -1; i--)
            {
                Renderer Particle = Renderers[i];

                Particle.RenderOffset.X += Wind + WeatherRandom.NextSingle();
                Particle.RenderOffset.Y += Gravity;

                if(Wind > 0)
                {
                    Particle.SpriteEffect = SpriteEffects.FlipHorizontally;
                } else
                {
                    Particle.SpriteEffect = SpriteEffects.None;
                }


                if (Particle.RenderOffset.Y > 5)
                {
                    if (Terra != null)
                    {
                        if (Terra.CheckCollision(new Rectangle((int)Particle.RenderOffset.X, (int)(Particle.RenderOffset.Y+Gravity), Particle.Sprite.Width, Particle.Sprite.Height), false))
                        {
                            switch (MaterialOnImpact)
                            {
                                case ImpactMaterial.Snow:
                                    Terra.MakeSnow((int)Particle.RenderOffset.X, (int)Particle.RenderOffset.Y, ImpactSpawnRadius);
                                    break;
                                case ImpactMaterial.Water:
                                    Terra.MakeWater((int)Particle.RenderOffset.X, (int)Particle.RenderOffset.Y, ImpactSpawnRadius);
                                    break;
                                default:
                                    break;
                            }

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
            if(Sprite == null)
            {
                return;
            }
            
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
