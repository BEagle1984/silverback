// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Consumer
{
    public abstract class ConsumerAppBase
    {
        private IBrokerCollection? _brokers;

        protected ConsumerAppBase()
        {
            ConsumerGroupName = GetType().Name;
        }

        public string ConsumerGroupName { get; set; }

        protected IServiceProvider? ServiceProvider { get; private set; }

        public void Start()
        {
            PromptForGroupName();
            WriteHeader();

            LoggingConfiguration.Setup();

            var services = DependencyInjectionHelper.GetServiceCollection(
                SqlServerConnectionHelper.GetConsumerConnectionString(GetType().Name));
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();
            ServiceProvider.GetRequiredService<ExamplesDbContext>().Database.EnsureCreated();
            _brokers = Configure(ServiceProvider.GetService<IBusConfigurator>());

            Console.CancelKeyPress += OnCancelKeyPress;

            WorkerHelper.LoopUntilCancelled();
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract IBrokerCollection Configure(IBusConfigurator busConfigurator);

        protected virtual void Exit()
        {
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;

            Exit();

            _brokers?.Disconnect();

            Console.CancelKeyPress -= OnCancelKeyPress;
        }

        private void PromptForGroupName()
        {
            Console.Write("Please choose the desired ");
            Console.ForegroundColor = Constants.AccentColor;
            Console.Write("consumer group name");
            ConsoleHelper.ResetColor();
            Console.Write(" or press ENTER to use ");
            Console.ForegroundColor = Constants.AccentColor;
            Console.Write($"{ConsumerGroupName}");
            ConsoleHelper.ResetColor();
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
            Console.WriteLine("(press CTRL-C to exit)");
            Console.WriteLine();
            ConsoleHelper.ResetColor();
        }
    }
}
