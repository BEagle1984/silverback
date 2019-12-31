// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Common.Consumer
{
    public abstract class ConsumerApp
    {
        private IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private IBroker _broker;

        public string ConsumerGroupName { get; set; }

        protected ConsumerApp()
        {
            ConsumerGroupName = GetType().Name;
        }

        public void Start()
        {
            PromptForGroupName();
            WriteHeader();

            LoggingConfiguration.Setup();

            var services = DependencyInjectionHelper.GetServiceCollection(
                SqlServerConnectionHelper.GetConsumerConnectionString(ConsumerGroupName));
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.GetRequiredService<ExamplesDbContext>().Database.EnsureCreated();
            _broker = Configure(_serviceProvider.GetService<BusConfigurator>(), _serviceProvider);

            Console.CancelKeyPress += OnCancelKeyPress;

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    Task.Delay(5000, _cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract IBroker Configure(BusConfigurator configurator, IServiceProvider serviceProvider);

        private void OnCancelKeyPress(object _, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            _broker?.Disconnect();
            _cancellationTokenSource.Cancel();

            Console.CancelKeyPress -= OnCancelKeyPress;
        }

        private void PromptForGroupName()
        {
            Console.Write("Please choose the desired ");
            Console.ForegroundColor = Constants.AccentColor;
            Console.Write("consumer group name");
            Console.ResetColor();
            Console.Write(" or press ENTER to use ");
            Console.ForegroundColor = Constants.AccentColor;
            Console.Write($"{ConsumerGroupName}");
            Console.ResetColor();
            Console.WriteLine(":");
            Console.CursorVisible = true;

            var input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
                ConsumerGroupName = input;

            Console.CursorVisible = false;
        }

        private void WriteHeader()
        {
            Console.WriteLine($"Initializing {GetType().Name} (group: {ConsumerGroupName})...");
            Console.ForegroundColor = Constants.SecondaryColor;
            Console.WriteLine($"(press CTRL-C to exit)");
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}