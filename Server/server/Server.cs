using Server.client;
using Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    internal static class Server
    {
        public static int max_clients { get; private set; }
        public static int port { get; private set; }
        public static Dictionary<int, Client> clients = new();
        public delegate void PacketHandler(int client_id, Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static Socket TCPListner;
        private static Socket UDPListner;
        private static IPEndPoint ServerEndpoint;

        public static void Start(int maximum_clients, int port_no)
        {
            max_clients = maximum_clients;
            port = port_no;
            InitializePacketHandlers();

            ServerEndpoint = new IPEndPoint(IPAddress.Any, port);
            TCPListner = new Socket(SocketType.Stream, ProtocolType.Tcp);
            TCPListner.Bind(ServerEndpoint);
            TCPListner.Listen(128);

            _ = Task.Run(() => TCPListen(TCPListner));
            UDPListner = new Socket(SocketType.Dgram, ProtocolType.Udp);
            UDPListner.Bind(ServerEndpoint);
            _ = Task.Run(() => UDPListen(UDPListner));
        }

        private static async Task UDPListen(Socket socket)
        {
            while (true)
            {
                try
                {
                    byte[] data = new byte[4096];
                    var socketReceive = await socket.ReceiveFromAsync(data, SocketFlags.None, new IPEndPoint(IPAddress.Any, 7787)).ConfigureAwait(false);
                    if (socketReceive.ReceivedBytes < 4)
                    {
                        return;
                    }
                    using (Packet packet = new Packet(data))
                    {
                        int clientId = packet.ReadInt();
                        if (clientId <= 0 || clientId > max_clients)
                        {
                            return;
                        }
                        if (clients[clientId].Udp.endPoint == null)
                        {
                            clients[clientId].Udp.Connect(socketReceive.RemoteEndPoint);
                            return;
                        }
                        else
                        {
                            //TODO: handle this condition either disconnect client or something
                        }
                        if (socketReceive.RemoteEndPoint.ToString() == clients[clientId].Udp.endPoint.ToString())
                        {
                            clients[clientId].Udp.HandleData(packet);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error encountered while listening for udp packet: {e}");
                }
            }
        }

        public static async Task SendUDPData(EndPoint endPoint, Packet packet)
        {
            try
            {
                if(endPoint != null)
                {
                    await UDPListner.SendToAsync(packet.ToArray(), SocketFlags.None, endPoint).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error encountered while sending UDP data to {endPoint} : {e}");
            }
        }

        private static async Task TCPListen(Socket socket)
        {
            while (true)
            {
                var item = await Task.Factory.FromAsync(socket.BeginAccept, socket.EndAccept, socket).ConfigureAwait(false);
                Console.WriteLine("Client Connected");
                await AddClient(item).ConfigureAwait(false);
            }
        }

        private static async Task AddClient(Socket socket)
        {
            for (int i = 1; i <= max_clients; i++)
            {
                if (!clients.ContainsKey(i))
                {
                    var client = new Client(i);
                    client.Tcp.Connect(socket);
                    clients.Add(i, client);
                    await ServerSend.Welcome(i, "Welcome to server").ConfigureAwait(false);
                    return;
                }
            }

            Console.WriteLine($"${socket.RemoteEndPoint} failed to connect, too many peoples");
        }

        private static void InitializePacketHandlers()
        {
            packetHandlers = new Dictionary<int, PacketHandler>
            {
                { (int)ServerPackets.welcome, ServerHandle.WelcomeReceived },

            };
        }
    }
}
