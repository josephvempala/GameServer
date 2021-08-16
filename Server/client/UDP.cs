using Shared;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
            await Server.SendUDPData(endPoint, packet).ConfigureAwait(false);
        }

        public void HandleData(Packet packet)
        {
            int packetLength = packet.ReadInt();
            byte[] data = packet.ReadBytes(packetLength);

            TickManager.ExecuteOnTick(() =>
            {
                using (Packet _packet = new Packet(data))
                {
                    int packetId = packet.ReadInt();
                    Server.packetHandlers[packetId].Invoke(id, packet);
                }
            });
        }

        public void Disconnect()
        {
            
        }
    }
}
