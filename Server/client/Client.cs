

using System;
using System.Numerics;

namespace Server.client
{
    public class Client
    {
        private int id;
        public TCP Tcp;
        public UDP Udp;
        public Player player;

        public Client(int client_id)
        {
            id = client_id;
            Tcp = new TCP(id);
            Udp = new UDP(id);
        }

        public void SendIntoGame(string playername)
        {
            player = new Player(id, playername, new Vector3(0,25f,0));

            foreach (var client in Server.clients.Values)
            {
                if(client.player != null && client.id != id)
                {
                    ServerSend.SpawnPlayer(id, client.player);
                }
            }

            foreach (var client in Server.clients.Values)
            {
                if (client.player != null)
                {
                    ServerSend.SpawnPlayer(client.id, player);
                }
            }
        }

        public void Disconnect()
        {
            player = null;
            Tcp.Disconnect();
            Udp.Disconnect();

            Server.clients.Remove(id);
            Console.WriteLine("Client " + id + " has disconnected");
        }
    }
}
