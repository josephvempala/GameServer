using System;
using System.Net;
using System.Threading.Tasks;

namespace Client
{
    internal static class Program
    {
        public static bool isRunning = false;
        private static void Main(string[] args)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1]));
            Client.Connect(endpoint);
            isRunning = true;
            _ = Task.Run(StartUpdateTicks);
            while (true)
            {
                string message = Console.ReadLine();
                ClientSend.SendMessage(message);
            }
        }
        public static async Task StartUpdateTicks()
        {
            Console.WriteLine("started client ticks");
            var nextloop = DateTime.Now;
            while (isRunning)
            {
                while (nextloop < DateTime.Now)
                {
                    TickManager.StartTick();

                    nextloop = nextloop.AddMilliseconds(1000 / 30);

                    if (nextloop > DateTime.Now)
                    {
                        await Task.Delay(nextloop - DateTime.Now).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
