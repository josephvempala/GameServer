using Server.client;
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
        private EndPoint serverEndPoint;
        public EndPoint localEndPoint;

        public void Connect(EndPoint ServerEndPoint)
        {
            try
            {
                serverEndPoint = ServerEndPoint;
                socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(localEndPoint);
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
                    byte[] udpBuffer = ArrayPool<byte>.Shared.Rent(Constants.MAX_BUFFER_SIZE);
                    SocketReceiveFromResult socketData = await SocketTaskExtensions.ReceiveFromAsync(socket, new ArraySegment<byte>(udpBuffer), SocketFlags.None, localEndPoint).ConfigureAwait(false);
                    if (socketData.ReceivedBytes < 4)
                    {
                        continue;
                    }
                    byte[] received_buffer = ArrayPool<byte>.Shared.Rent(socketData.ReceivedBytes);
                    Buffer.BlockCopy(udpBuffer, 0, received_buffer, 0, socketData.ReceivedBytes);
                    ArrayPool<byte>.Shared.Return(udpBuffer);
                    HandleData(udpBuffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception encountered when Receiving UDP packet {ex}");
                    return;
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
            Packet packet = new Packet(data);
            int packetId = packet.ReadInt();
            if(Client.packetHandlers.ContainsKey(packetId))
                TickManager.ExecuteOnTick(() =>
                {
                    Client.packetHandlers[packetId].Invoke(packet);
                    ArrayPool<byte>.Shared.Return(data);
                    packet.Dispose();
                });

        }
    }
}
