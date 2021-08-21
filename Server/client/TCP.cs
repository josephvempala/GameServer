using Shared;
using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server.client
{
    public class TCP
    {
        private Socket socket;
        private NetworkStream stream;
        private Packet receivedData;
        private int id;

        public TCP(int id)
        {
            this.id = id;
        }

        public void Connect(Socket socket)
        {
            this.socket = socket;
            socket.ReceiveBufferSize = Constants.MAX_BUFFER_SIZE;
            socket.SendBufferSize = Constants.MAX_BUFFER_SIZE;

            stream = new NetworkStream(socket);

            _ = Task.Run(ReceiveLoop);
        }

        public async Task SendAsync(Packet packet)
        {
            byte[] buffer = packet.ToArray();
            await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        private async Task ReceiveLoop()
        {
            while (true)
            {
                receivedData = new Packet();
                byte[] receiveBuffer = ArrayPool<byte>.Shared.Rent(Constants.MAX_BUFFER_SIZE);
                int bytes_read = await stream.ReadAsync(receiveBuffer, 0, Constants.MAX_BUFFER_SIZE).ConfigureAwait(false);
                if (bytes_read == 0)
                {
                    Server.clients[id].Disconnect();
                    continue;
                }

                byte[] data_read = ArrayPool<byte>.Shared.Rent(bytes_read);
                Array.Copy(receiveBuffer, data_read, bytes_read);

                receivedData.Reset(HandleData(data_read));
            }
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
                if(Server.packetHandlers.ContainsKey(packet_id))
                    TickManager.ExecuteOnTick(() =>
                    {
                        Server.packetHandlers[packet_id].Invoke(id, packet);
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

        public void Disconnect()
        {
            socket.Close();
            stream.Close();
            receivedData.Dispose();
        }
    }
}
