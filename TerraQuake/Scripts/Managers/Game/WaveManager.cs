using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    public static class WaveManager
    {
        public static bool IsWaveActive = true;
        public static TimeSpan NextBalloon = new TimeSpan();
        public static float SpawnRate = 1.1f;

        public static float GetSpawnRate()
        {
            System.Random RNG = new System.Random();
            return SpawnRate * RNG.Next(1, 4);
        }

        public static void Update(GameTime gameTime)
        {
            if (IsWaveActive)
            {
                if(gameTime.TotalGameTime > NextBalloon)
                {
                    NextBalloon = gameTime.TotalGameTime + TimeSpan.FromSeconds(GetSpawnRate());
                    //ContentManager.Game.CreateBalloon();
                }
            }
        }
    }
}
