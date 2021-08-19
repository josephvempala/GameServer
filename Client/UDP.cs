using Shared;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client
{
    internal class UDP
    {
        private Socket socket;
        private EndPoint localEndPoint;
        private EndPoint serverEndPoint;

        public void Connect(EndPoint LocalEndPoint, EndPoint ServerEndPoint)
        {
            try
            {
                localEndPoint = LocalEndPoint;
                serverEndPoint = ServerEndPoint;
                socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(LocalEndPoint);
                using (Packet packet = new Packet())
                {
                    Send(packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception encountered while connecting to UDP server {ex}");
            }
            _ = Task.Run(Receive);
        }


        private async Task Receive()
        {
            while (true)
            {
                try
                {
                    byte[] udpBuffer = ArrayPool<byte>.Shared.Rent(4096);
                    var socketData = await SocketTaskExtensions.ReceiveFromAsync(socket, new ArraySegment<byte>(udpBuffer), SocketFlags.None, localEndPoint).ConfigureAwait(false);
                    if (socketData.ReceivedBytes < 4)
                    {
                        continue;
                    }
                    var received_buffer = new byte[socketData.ReceivedBytes];
                    Array.Copy(udpBuffer, received_buffer, socketData.ReceivedBytes);
                    ArrayPool<byte>.Shared.Return(udpBuffer);
                    HandleData(udpBuffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception encountered when Receiving UDP packet {ex}");
                }
            }
        }

        public void Send(Packet packet)
        {
            if (socket == null)
            {
                Console.WriteLine($"Call Connect(IPEndpoint ep) on the UDP object with an IPEndpoint as parameter before calling Send(Packet p)");
                return;
            }
            try
            {
                packet.InsertInt(Client.id);
                _ = SocketTaskExtensions.SendToAsync(socket, new ArraySegment<byte>(packet.ToArray()), SocketFlags.None, serverEndPoint).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception encountered when Sending UDP packet {ex}");
            }

        }

        public void Disconnect()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            localEndPoint = null;
        }

        private void HandleData(byte[] data)
        {
            TickManager.ExecuteOnTick(() =>
            {
                using (Packet packet = new Packet(data))
                {
                    var packetId = packet.ReadInt();
                    Client.packetHandlers[packetId].Invoke(packet);
                }
            });
        }
    }
}
