using Server.client;
using Shared;
using System;
using System.Buffers;
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
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
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

            _ = Task.Run(TCPListen);
            UDPListner = new Socket(SocketType.Dgram, ProtocolType.Udp);
            UDPListner.Bind(ServerEndpoint);
            _ = Task.Run(UDPListen);
        }

        public static async Task SendUDPDataAsync(EndPoint endPoint, Packet packet)
        {
            try
            {
                if (endPoint != null)
                {
                    await SocketTaskExtensions.SendToAsync(UDPListner, new ArraySegment<byte>(packet.ToArray()), SocketFlags.None, endPoint).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error encountered while sending UDP data to {endPoint} : {e}");
            }
        }

        private static async Task UDPListen()
        {
            while (true)
            {
                try
                {
                    ArraySegment<byte> data = new ArraySegment<byte>(ArrayPool<byte>.Shared.Rent(4096));
                    var socketReceive = await SocketTaskExtensions.ReceiveFromAsync(UDPListner, data, SocketFlags.None, new IPEndPoint(IPAddress.Any, 7787)).ConfigureAwait(false);
                    if (socketReceive.ReceivedBytes < 4)
                    {
                        continue;
                    }
                    int clientId = BitConverter.ToInt32(data.Array, 0);
                    if (clientId <= 0 || clientId > max_clients)
                    {
                        continue;
                    }
                    if (clients[clientId].Udp.endPoint == null)
                    {
                        clients[clientId].Udp.Connect(socketReceive.RemoteEndPoint);
                        continue;
                    }
                    if (socketReceive.RemoteEndPoint.ToString() == clients[clientId].Udp.endPoint.ToString())
                    {
                        var packetBytes = new byte[socketReceive.ReceivedBytes];
                        Array.Copy(data.Array, 0, packetBytes, 0, socketReceive.ReceivedBytes);
                        ArrayPool<byte>.Shared.Return(data.Array);
                        clients[clientId].Udp.HandleData(packetBytes);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error encountered while listening for udp packet: {e}");
                }
            }
        }

        private static async Task TCPListen()
        {
            while (true)
            {
                var item = await Task.Factory.FromAsync(TCPListner.BeginAccept, TCPListner.EndAccept, TCPListner).ConfigureAwait(false);
                Console.WriteLine("Client Connected");
                AddClient(item);
            }
        }

        private static void AddClient(Socket socket)
        {
            for (int i = 1; i <= max_clients; i++)
            {
                if (!clients.ContainsKey(i))
                {
                    var client = new Client(i);
                    client.Tcp.Connect(socket);
                    clients.Add(i, client);
                    ServerSend.Welcome(i, "Welcome to server");
                    return;
                }
            }

            Console.WriteLine($"${socket.RemoteEndPoint} failed to connect, too many peoples");
        }

        private static void InitializePacketHandlers()
        {
            packetHandlers = new Dictionary<int, PacketHandler>
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.message, ServerHandle.MessageReceived }
            };
        }
    }
}
