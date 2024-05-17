using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraQuake.Networking
{
    internal class ServerHandler
    {
        public static void WELCOME(int _fromClient, Packet _packet) 
        {
            string WelcomeMessage = _packet.ReadString();
            Terrain Terra = ContentManager.Game.TerrainInstance;
            using (Packet Pack = new Packet((int)PacketsIDs.TERRAININIT))
            {
                Pack.Write(Terra.Seed);
                Pack.Write(Settings.DefaultTerrainW);
                Pack.Write(Settings.DefaultTerrainW);
                Pack.Write((int)Settings.TerrainStyle);
                Network.GetServer().SendData(Pack);
            }
        }
    }
}
