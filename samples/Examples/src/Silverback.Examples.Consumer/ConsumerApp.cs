// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Examples.Common;
using Silverback.Examples.Common.TestHost;
using Silverback.Examples.Consumer.Configuration;
using Silverback.Examples.Consumer.HostedServices;

namespace Silverback.Examples.Consumer
{
    public sealed class ConsumerApp : IDisposable
    {
        private TestApplicationHost<Startup>? _host;

        private string _consumerGroupName;

        private CancellationTokenSource? _stoppingTokenSource;

        public ConsumerApp()
        {
            _consumerGroupName = GetType().Name;
        }

        public async Task Run()
        {
            if (_host != null)
                throw new InvalidOperationException("This consumer app is already running.");

            Console.CancelKeyPress += OnCancelKeyPress;

            PromptForGroupName();
            WriteHeader();

            _host = new TestApplicationHost<Startup>();
            _host.ConfigureServices(
                services => services.Configure<ConsumerGroupConfiguration>(
                    configuration => { configuration.ConsumerGroupName = _consumerGroupName; }));
            _host.Run();

            await LoopUntilStopped();
        }

        public void Stop()
        {
            var watcher = _host?.ServiceProvider
                .GetServices<IHostedService>()
                .OfType<ConsumersWatcher>()
                .FirstOrDefault();

            watcher?.StopAsync(CancellationToken.None);

            _host?.Dispose();
            _host = null;

            _stoppingTokenSource?.Cancel();
            _stoppingTokenSource?.Dispose();
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task LoopUntilStopped()
        {
            _stoppingTokenSource = new CancellationTokenSource();

            while (!_stoppingTokenSource.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, _stoppingTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;

            Console.CancelKeyPress -= OnCancelKeyPress;

            Stop();
        }

        private void PromptForGroupName()
        {
            Console.Write("Please choose the desired ");
            Console.ForegroundColor = Constants.AccentColor;
            Console.Write("consumer group name");
            ConsoleHelper.ResetColor();
            Console.Write(" or press ENTER to use ");
            Console.ForegroundColor = Constants.AccentColor;
            Console.Write($"{_consumerGroupName}");
            ConsoleHelper.ResetColor();
            Console.WriteLine(":");
            Console.CursorVisible = true;

            var input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
                _consumerGroupName = input;

            Console.CursorVisible = false;
        }

        private void WriteHeader()
        {
            Console.WriteLine($"Initializing {GetType().Name} (group: {_consumerGroupName})...");
            Console.ForegroundColor = Constants.SecondaryColor;
            Console.WriteLine("(press CTRL-C to exit)");
            Console.WriteLine();
            ConsoleHelper.ResetColor();
        }
    }
}
