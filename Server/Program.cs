using System;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        private static bool isRunning = false;

        public static void Main(string[] args)
        {
            Server.Start(int.Parse(args[0]), int.Parse(args[1]));
            isRunning = true;
            _ = Task.Run(StartUpdateTicks);
            Console.ReadKey();
        }

        public static async Task StartUpdateTicks()
        {
            Console.WriteLine("started server ticks");
            DateTime nextloop = DateTime.Now;
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
