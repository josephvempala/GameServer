using Shared;
using System.Threading.Tasks;

namespace Server
{
    public static class ServerSend
    {
        private static async Task SendTCPPacket(int clientId, Packet packet)
        {
            packet.WriteLength();
            await Server.clients[clientId].Tcp.SendAsync(packet).ConfigureAwait(false);
        }

        private static async Task SendUDPPacket(int clientId, Packet packet)
        {
            packet.WriteLength();
            await Server.clients[clientId].Udp.SendAsync(packet).ConfigureAwait(false);
        }

        public static async Task Welcome(int clientId, string message)
        {
            using (Packet packet = new Packet((int)ServerPackets.welcome))
            {
                packet.Write(message);
                await SendTCPPacket(clientId, packet).ConfigureAwait(false);
            }
        }

        public static async Task UdpTest(int clientId, string message)
        {
            using (Packet packet = new Packet((int)ServerPackets.udpTest))
            {
                packet.Write(message);
                await SendUDPPacket(clientId, packet).ConfigureAwait(false);
            }
        }
    }
}
