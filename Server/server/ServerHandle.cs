using Shared;
using System;

namespace Server
{
    internal static class ServerHandle
    {
        public static void WelcomeReceived(int client_id, Packet packet)
        {
            string message = packet.ReadString();
            Console.WriteLine($"{client_id} says {message}");
        }

        public static void MessageReceived(int client_id, Packet packet)
        {
            string message = packet.ReadString();
            Console.WriteLine($"{message} from udp client {client_id}");
            ServerSend.Message(client_id, message);
        }
    }
}
