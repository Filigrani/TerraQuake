using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerraQuake
{
    internal class Network
    {
        public static int DefaultPort = 33778;
        public static int DataBufferSize = 4096;
        public static int ProtocolVersion = 1;

        public static void Test()
        {
            Server TestServer = new Server();
            TestServer.Initialize();

            Client TestClient = new Client();
            TestClient.Initialize();
            TestClient.Connect("127.0.0.1", DefaultPort);
        }
        
        public class Server
        {
            public TcpListener Listener = null;
            public bool Initialized = false;
            public int Port = DefaultPort;
            public Dictionary<int, ServerClient> Clients = new Dictionary<int, ServerClient>();
            public int MaxPlayers = 2;

            public void Initialize()
            {
                if(!Initialized)
                {
                    Listener = new TcpListener(System.Net.IPAddress.Any, Port);
                    Listener.Start();
                    Listener.BeginAcceptTcpClient(new AsyncCallback(HandleClient), null);
                }

                for (int i = 1; i <= MaxPlayers; i++)
                {
                    Clients.Add(i, new ServerClient(i, this));
                }
            }

            public void HandleData(byte[] Data)
            {
                using (Packet Pack = new Packet(Data))
                {
                    int Length = Pack.ReadInt();
                    int ProtocolVersion = Pack.ReadInt();
                    int PacketID = Pack.ReadInt();

                    if(PacketID == (int)ClientPackets.WELCOME)
                    {
                        string WelcomeAnswer = Pack.ReadString();
                        int Good = 0;
                    }
                }
            }

            public void SendData(Packet Pak, int ID)
            {
                Clients[ID].SendData(Pak);
            }
            public void SendData(Packet Pak)
            {
                for (int ID = 1; ID <= MaxPlayers; ID++)
                {
                    Clients[ID].SendData(Pak);
                }
            }

            public void WelcomePlayer(int ID)
            {
                using (Packet Pack = new Packet((int)ClientPackets.WELCOME))
                {
                    Pack.Write("Yo!");
                    SendData(Pack, ID);
                }
            }
            public void HandleClient(IAsyncResult _result)
            {
                TcpClient client = Listener.EndAcceptTcpClient(_result);
                Listener.BeginAcceptTcpClient(new AsyncCallback(HandleClient), null);

                for (int i = 1; i <= MaxPlayers; i++)
                {
                    if (Clients[i].Socket == null)
                    {
                        Clients[i].Connect(client);
                        WelcomePlayer(i);
                        return;
                    }
                }
                //Server Full!
            }

            public class ServerClient
            {
                public TcpClient Socket;
                public int ID;
                public NetworkStream DataStream;
                public byte[] Buffer;
                public Server Server;

                public ServerClient(int id, Server server)
                {
                    ID = id;
                    Server = server;
                }

                public void Connect(TcpClient socket)
                {
                    Socket = socket;
                    Socket.ReceiveBufferSize = DataBufferSize;
                    Socket.SendBufferSize = DataBufferSize;

                    DataStream = Socket.GetStream();

                    Buffer = new byte[DataBufferSize];

                    DataStream.BeginRead(Buffer, 0, DataBufferSize, ReceiveCallback, null);
                }

                public void SendData(Packet Pack)
                {
                    Pack.InsertInt(ProtocolVersion);
                    Pack.WriteLength();
                    if (Socket != null)
                    {
                        try
                        {
                            DataStream.BeginWrite(Pack.ToArray(), 0, Pack.Length(), null, null);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                private void ReceiveCallback(IAsyncResult _result)
                {
                    try
                    {
                        int _byteLength = DataStream.EndRead(_result);
                        if (_byteLength <= 0)
                        {
                            return;
                        }

                        byte[] _data = new byte[_byteLength];
                        Array.Copy(Buffer, _data, _byteLength);
                        Server.HandleData(_data);
                        DataStream.BeginRead(Buffer, 0, DataBufferSize, ReceiveCallback, null);
                    }
                    catch (Exception _ex)
                    {

                    }
                }
            }
        }

        public class Client
        {
            public TcpClient Socket;
            public bool Initialized = false;
            public int Port = DefaultPort;
            public NetworkStream DataStream;
            public byte[] Buffer;

            public void Initialize()
            {
                if (!Initialized)
                {
                    Socket = new TcpClient
                    {
                        ReceiveBufferSize = DataBufferSize,
                        SendBufferSize = DataBufferSize
                    };
                    Buffer = new byte[DataBufferSize];

                    Initialized = true;
                }
            }

            public void Connect(string IP, int Port)
            {
                if (Initialized)
                {
                    Socket.BeginConnect(IP, Port, ConnectCallback, Socket);
                }
            }

            private void ConnectCallback(IAsyncResult _result)
            {
                Socket.EndConnect(_result);

                if (!Socket.Connected)
                {
                    return;
                }

                DataStream = Socket.GetStream();

                DataStream.BeginRead(Buffer, 0, DataBufferSize, ReceiveCallback, null);
            }


            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = DataStream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        return;
                    }

                    byte[] _data = new byte[_byteLength];
                    Array.Copy(Buffer, _data, _byteLength);
                    HandleData(_data);
                    DataStream.BeginRead(Buffer, 0, DataBufferSize, ReceiveCallback, null);
                }
                catch
                {

                }
            }

            public void SendData(Packet Pack)
            {
                Pack.InsertInt(ProtocolVersion);
                Pack.WriteLength();
                if (Socket != null)
                {
                    try
                    {
                        DataStream.BeginWrite(Pack.ToArray(), 0, Pack.Length(), null, null);
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            public void WelcomeAnswer()
            {
                using (Packet Pack = new Packet((int)ClientPackets.WELCOME))
                {
                    Pack.Write("Joined!");
                    SendData(Pack);
                }
            }

            public void HandleData(byte[] Data)
            {
                using (Packet Pack = new Packet(Data))
                {
                    int Length = Pack.ReadInt();
                    int ProtocolVersion = Pack.ReadInt();
                    int PacketID = Pack.ReadInt();

                    if (PacketID == (int)ClientPackets.WELCOME)
                    {
                        string WelcomeMessage = Pack.ReadString();
                        WelcomeAnswer();
                    }
                }
            }
        }
    }
}
