using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class Settings
    {
        public static int DefaultTerrainW = 2000;
        public static int DefaultTerrainH = 900;
        public static int TerrainSeed = 0;
        public static WeatherParticles.ImpactMaterial Weather = WeatherParticles.ImpactMaterial.None;
        public static Terrain.ChunksDebug ChunkDebugMode = Terrain.ChunksDebug.None;
        public static Terrain.GenerationStyle TerrainStyle = Terrain.GenerationStyle.Simple;
    }
}
