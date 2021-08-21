using Shared;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;

namespace Server.client
{
    public class UDP
    {
        private int id;
        public EndPoint endPoint;

        public UDP(int id)
        {
            this.id = id;
        }

        public void Connect(EndPoint endpoint)
        {
            endPoint = endpoint;
        }

        public async Task SendAsync(Packet packet)
        {
            await Server.SendUDPDataAsync(endPoint, packet).ConfigureAwait(false);
        }

        public void HandleData(byte[] data)
        {
            Packet packet = new Packet(data);
            int clientId = packet.ReadInt();
            int packetId = packet.ReadInt();
            if(Server.packetHandlers.ContainsKey(packetId))
                TickManager.ExecuteOnTick(() =>
                {
                    Server.packetHandlers[packetId].Invoke(id, packet);
                    ArrayPool<byte>.Shared.Return(data);
                    packet.Dispose();
                });
        }

        public void Disconnect()
        {
            endPoint = null;
        }
    }
}
