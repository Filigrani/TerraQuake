using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake.Networking
{
    internal class ClientHandler
    {
        public static void WELCOME(int _fromClient, Packet _packet)
        {
            using (Packet Pack = new Packet((int)PacketsIDs.WELCOME))
            {
                Pack.Write("Joined!");
                Network.GetClient().SendData(Pack);
            }
        }
        public static void TERRAININIT(int _fromClient, Packet _packet)
        {
            int Seed = _packet.ReadInt();
            Settings.DefaultTerrainW = _packet.ReadInt();
            Settings.DefaultTerrainW = _packet.ReadInt();
            Settings.TerrainStyle = (Terrain.GenerationStyle)_packet.ReadInt();
            Settings.TerrainSeed = Seed;
            LevelManager.StartLevel("Game");
        }
        public static void MAKEHOLE(int _fromClient, Packet _packet)
        {
            Terrain Terra = ContentManager.Game.TerrainInstance;
            Terra.ResumeHistory();
            Terra.AddHoleToHistory(_packet.ReadInt(), _packet.ReadInt(), _packet.ReadInt(), _packet.ReadInt());
        }
    }
}
