using Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        private static NetworkStream stream;
        private static byte[] receiveBuffer = new byte[4096];
        private static Packet receivedData;
        public delegate void PacketHandler(Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        public static Socket TCPSocket;
        public static Socket UDPSocket;
        public static EndPoint endpoint;

        private static void Main(string[] args)
        {
            endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7787);
            InitializePacketHandlers();
            TCPSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            TCPSocket.Connect(endpoint);
            stream = new NetworkStream(TCPSocket);
            _ = Task.Run(Receive);
            UDPSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            _ = Task.Run(ReceiveUDP);

            Console.ReadKey();
        }
        private static async Task ReceiveUDP()
        {
            while (true)
            {
                byte[] udpBuffer = new byte[4096];
                var socketData = await UDPSocket.ReceiveFromAsync(udpBuffer, SocketFlags.None, endpoint);
                if(socketData.ReceivedBytes < 4)
                {
                    return;
                }
                using (Packet packet = new Packet(udpBuffer))
                {
                    var packetId = packet.ReadInt();
                    packetHandlers[packetId].Invoke(packet);
                }
            }
        }
        private static async Task Receive()
        {
            while (true)
            {
                receivedData = new Packet();
                try
                {
                    int bytes_read = await stream.ReadAsync(receiveBuffer, 0, 4096);
                    if (bytes_read == 0)
                    {
                        //handle disconnect
                        return;
                    }

                    byte[] data_read = new byte[bytes_read];
                    Array.Copy(receiveBuffer, data_read, bytes_read);

                    receivedData.Reset(HandleData(data_read));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public async Task SendAsync(Packet packet)
        {
            var buffer = packet.ToArray();
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static void InitializePacketHandlers()
        {
            packetHandlers = new Dictionary<int, PacketHandler>
            {
               {(int)ClientPackets.welcomeReceived, ClientHandle.Welcome },
               {(int)ClientPackets.udpTestReceived, ClientHandle.UDPTest }
            };
        }

        private static bool HandleData(byte[] data)
        {
            int packet_length = 0;
            receivedData.SetBytes(data);

            if (receivedData.UnreadLength >= 4)
            {
                packet_length = receivedData.ReadInt();
                if (packet_length == 0)
                {
                    return true;
                }
            }
            while (packet_length > 0 && packet_length <= receivedData.UnreadLength)
            {
                byte[] packet_Bytes = receivedData.ReadBytes(packet_length);
                using (Packet packet = new Packet(packet_Bytes))
                {
                    int packet_id = packet.ReadInt();
                    packetHandlers[packet_id].Invoke(packet);
                }
                packet_length = 0;
                if (receivedData.UnreadLength >= 4)
                {
                    packet_length = receivedData.ReadInt();
                    if (packet_length == 0)
                    {
                        return true;
                    }
                }
            }
            if (packet_length <= 1)
            {
                return true;
            }
            return false;
        }
    }

    internal static class ClientHandle
    {
        public static void Welcome(Packet packet)
        {
            string item = packet.ReadString();
            Console.WriteLine($"Server says {item}");
        }
        public static void UDPTest(Packet packet)
        {
            string item = packet.ReadString();
            Console.WriteLine($"Server says through UDP {item}");
        }
    }

    internal static class ClientSend
    {
        private static async Task SendTCPData(Packet packet)
        {
            packet.WriteLength();
            await SendTCPData(packet);
        }

        public static async Task WelcomeReceived(string message)
        {
            using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
            {
                packet.Write(message);
                await SendTCPData(packet);
            }
        }
    }
}
