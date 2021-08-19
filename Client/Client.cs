using Shared;
using System.Collections.Generic;
using System.Net;

namespace Client
{
    internal static class Client
    {
        public delegate void PacketHandler(Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        public static IPEndPoint ServerEndpoint;
        public static TCP tcp = new TCP();
        public static UDP udp = new UDP();
        public static int id;

        public static void Connect(IPEndPoint endPoint)
        {
            ServerEndpoint = endPoint;
            InitializePacketHandlers();
            tcp.Connect(ServerEndpoint);
        }

        public static void Disconnect()
        {
            tcp.Disconnect();
            udp.Disconnect();
        }

        private static void InitializePacketHandlers()
        {
            packetHandlers = new Dictionary<int, PacketHandler>
            {
               {(int)ServerPackets.welcome, ClientHandle.Welcome },
               {(int)ServerPackets.message, ClientHandle.Message }
            };
        }
    }
}
