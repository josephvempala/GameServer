using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        private static CancellationTokenSource cancellationTokenSource;

        public static void Main(string[] args)
        {
            Server.Start(int.Parse(args[0]), int.Parse(args[1]));
            cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            _ = Task.Run(StartUpdateTicks);
            Console.ReadKey();
        }

        public static void OnProcessExit(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
            Server.Stop();
            Console.WriteLine("Server Stopped");
        }

        public static async Task StartUpdateTicks()
        {
            Console.WriteLine("started server ticks");
            DateTime nextloop = DateTime.Now;
            while (!cancellationTokenSource.Token.IsCancellationRequested)
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
