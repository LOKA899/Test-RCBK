using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace lok_wss
{
    internal class Program
    {
        private static IServiceProvider _services;

        private static void Main()
        {
            _services = ConfigureServices();

            Thread scanThread = new Thread(() =>
            {
                ContinentScanner continentScanner = new ContinentScanner(61);
            });
            scanThread.Start();

            var thread = new Thread(() => { while (true) { Thread.Sleep(300000); } });
            thread.Start();
        }

        private static IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<Program>()
                .AddDbContext<lokContext>(ServiceLifetime.Scoped)
                .BuildServiceProvider();
        }
    }
}