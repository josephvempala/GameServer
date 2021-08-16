

namespace Server.client
{
    public class Client
    {
        private int id;
        public TCP Tcp;
        public UDP Udp;

        public Client(int client_id)
        {
            id = client_id;
            Tcp = new TCP(id);
            Udp = new UDP(id);
        }

        public void Disconnect()
        {
            Tcp.Disconnect();
            Udp.Disconnect();
            Server.clients.Remove(id);
        }
    }
}
