using Shared;
using System;
using System.Threading.Tasks;

namespace Server
{
    internal static class ServerHandle
    {
        public static void WelcomeReceived(int client_id, Packet packet)
        {
            string message = packet.ReadString();
            Console.WriteLine($"{client_id} says {message}");
            ServerSend.UdpTest(client_id, message);
        }
    }
}
