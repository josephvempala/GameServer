using System;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        private static bool isRunning = false;

        public static void Main(string[] args)
        {
            //Server.Start(int.Parse(args[0]), int.Parse(args[1]));
            Server.Start(16, 7787);
            isRunning = true;
            _ = StartUpdateTicks();
            Console.ReadKey();
        }

        public static async Task StartUpdateTicks()
        {
            Console.WriteLine("started server ticks");
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
