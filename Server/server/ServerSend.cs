using Shared;

namespace Server
{
    public static class ServerSend
    {
        private static void SendTCPPacket(int clientId, Packet packet)
        {
            packet.WriteLength();
            _ = Server.clients[clientId].Tcp.SendAsync(packet).ConfigureAwait(false);
        }

        private static void SendTCPPacketToAllButClient(int clientId, Packet packet)
        {
            packet.WriteLength();
            foreach (System.Collections.Generic.KeyValuePair<int, client.Client> client in Server.clients)
            {
                if (client.Key != clientId)
                {
                    _ = client.Value.Tcp.SendAsync(packet).ConfigureAwait(false);
                }
            }
        }

        private static void SendTCPPacketToAll(Packet packet)
        {
            packet.WriteLength();
            foreach (System.Collections.Generic.KeyValuePair<int, client.Client> client in Server.clients)
            {
                _ = client.Value.Tcp.SendAsync(packet).ConfigureAwait(false);
            }
        }

        private static void SendUDPPacket(int clientId, Packet packet)
        {
            _ = Server.clients[clientId].Udp.SendAsync(packet).ConfigureAwait(false);
        }

        private static void SendUDPPacketToAllButClient(int clientId, Packet packet)
        {
            foreach (System.Collections.Generic.KeyValuePair<int, client.Client> client in Server.clients)
            {
                if (client.Key != clientId)
                {
                    _ = client.Value.Udp.SendAsync(packet).ConfigureAwait(false);
                }
            }
        }

        private static void SendUDPPacketToAll(Packet packet)
        {
            foreach (System.Collections.Generic.KeyValuePair<int, client.Client> client in Server.clients)
            {
                _ = client.Value.Udp.SendAsync(packet).ConfigureAwait(false);
            }
        }

        public static void Welcome(int clientId, string message)
        {
            using (Packet packet = new Packet((int)ServerPackets.welcome))
            {
                packet.Write(clientId);
                packet.Write(message);
                SendTCPPacket(clientId, packet);
            }
        }

        public static void Message(int clientId, string message)
        {
            using (Packet packet = new Packet((int)ServerPackets.message))
            {
                packet.Write(clientId);
                packet.Write(message);
                SendUDPPacketToAll(packet);
            }
        }
    }
}
