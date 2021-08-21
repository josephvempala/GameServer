using Shared;
using System;
using System.Net;

namespace Client
{
    internal static class ClientHandle
    {
        public static void Welcome(Packet packet)
        {
            Client.id = packet.ReadInt();
            string item = packet.ReadString();
            Console.WriteLine($"Server says {item}");
            ClientSend.WelcomeReceived("thanq for welcome");
            Client.udp.localEndPoint = Client.tcp.LocalEndPoint;
            Client.udp.Connect(Client.ServerEndpoint);
        }
        public static void Message(Packet packet)
        {
            int client_id = packet.ReadInt();
            string item = packet.ReadString();
            Console.WriteLine($"Client {client_id} says through UDP : {item}");
        }
    }
}
