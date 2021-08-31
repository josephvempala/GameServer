using Server.client;
using Shared;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client
{
    internal class TCP
    {
        private NetworkStream stream;
        private Packet receivedData;
        private Socket socket;
        public EndPoint LocalEndPoint;

        public void Connect(EndPoint endpoint)
        {
            try
            {
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(endpoint);
                LocalEndPoint = socket.LocalEndPoint;
                stream = new NetworkStream(socket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception encountered while connecting to TCP server {ex}");
                return;
            }
            _ = Task.Run(Receive);
        }

        public void Send(Packet packet)
        {
            if (socket == null)
            {
                Console.WriteLine($"Call Connect(IPEndpoint ep) on the TCP object with an IPEndpoint as parameter before calling Send(Packet p)");
                return;
            }
            try
            {
                byte[] buffer = packet.ToArray();
                _ = stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception encountered when Sending TCP packet {ex}");
            }
        }

        private async Task Receive()
        {
            while (true)
            {
                try
                {
                    receivedData = new Packet();
                    byte[] receiveBuffer = ArrayPool<byte>.Shared.Rent(Constants.MAX_BUFFER_SIZE);
                    int bytes_read = await stream.ReadAsync(receiveBuffer, 0, Constants.MAX_BUFFER_SIZE).ConfigureAwait(false);
                    if (bytes_read == 0)
                    {
                        Client.Disconnect();
                        continue;
                    }

                    byte[] data_read = ArrayPool<byte>.Shared.Rent(bytes_read);
                    Buffer.BlockCopy(receiveBuffer, 0, data_read, 0, bytes_read);
                    ArrayPool<byte>.Shared.Return(receiveBuffer);
                    receivedData.Reset(HandleData(data_read));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception encountered when Receiving TCP packet {ex}");
                }
            }
        }

        public void Disconnect()
        {
            stream.Close();
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            receivedData.Dispose();
        }

        private bool HandleData(byte[] data)
        {
            int packet_length = 0;
            receivedData.SetBytes(data);

            if (receivedData.UnreadLength >= 4)
            {
                packet_length = receivedData.ReadInt();
                if (packet_length == 0)
                {
                    ArrayPool<byte>.Shared.Return(data);
                    return true;
                }
            }
            while (packet_length > 0 && packet_length <= receivedData.UnreadLength)
            {
                byte[] packet_Bytes = receivedData.ReadBytes(packet_length);
                Packet packet = new Packet(packet_Bytes);
                int packet_id = packet.ReadInt();
                if (Client.packetHandlers.ContainsKey(packet_id))
                    TickManager.ExecuteOnTick(() =>
                    {
                        Client.packetHandlers[packet_id].Invoke(packet);
                        packet.Dispose();
                    });
                packet_length = 0;
                if (receivedData.UnreadLength >= 4)
                {
                    packet_length = receivedData.ReadInt();
                    if (packet_length == 0)
                    {
                        ArrayPool<byte>.Shared.Return(data);
                        return true;
                    }
                }
            }
            if (packet_length <= 1)
            {
                ArrayPool<byte>.Shared.Return(data);
                return true;
            }
            return false;
        }
    }
}