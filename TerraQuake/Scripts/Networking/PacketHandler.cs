using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraQuake.Networking;
using static TerraQuake.Network;

namespace TerraQuake
{
    internal class PacketHandler
    {
        public delegate void PacketHandlerMethod(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandlerMethod> packetHandlers;
        public static readonly List<Action> executeOnMainThread = new List<Action>();
        public static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
        public static bool actionToExecuteOnMainThread = false;

        public static void Init()
        {
            if (Network.IsHost())
            {
                packetHandlers = new Dictionary<int, PacketHandlerMethod>()
                {
                    { (int)PacketsIDs.WELCOME, ServerHandler.WELCOME },
                };
            }
            if (Network.IsClient())
            {
                packetHandlers = new Dictionary<int, PacketHandlerMethod>()
                {
                    { (int)PacketsIDs.WELCOME, ClientHandler.WELCOME },
                    { (int)PacketsIDs.TERRAININIT, ClientHandler.TERRAININIT },
                    { (int)PacketsIDs.MAKEHOLE, ClientHandler.MAKEHOLE },
                };
            }
        }
        public static void ExecuteOnMainThread(Action _action)
        {
            if (_action == null)
            {
                return;
            }

            lock (executeOnMainThread)
            {
                executeOnMainThread.Add(_action);
                actionToExecuteOnMainThread = true;
            }
        }
        public static void Update()
        {
            if (actionToExecuteOnMainThread)
            {
                executeCopiedOnMainThread.Clear();
                lock (executeOnMainThread)
                {
                    executeCopiedOnMainThread.AddRange(executeOnMainThread);
                    executeOnMainThread.Clear();
                    actionToExecuteOnMainThread = false;
                }

                for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
                {
                    executeCopiedOnMainThread[i]();
                }
            }
        }

        public static void HandlePacket(byte[] Data)
        {
            ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(Data))
                {
                    int Length = _packet.ReadInt();
                    int ProtocolVersion = _packet.ReadInt();
                    int PacketID = _packet.ReadInt();
                    packetHandlers[PacketID](PacketID, _packet);
                }
            });
        }
    }
}
