// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Examples.Common.Data;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.EfCore
{
    public class OutboundWorkerUseCase : UseCase
    {
        private CancellationTokenSource _cancellationTokenSource;

        public OutboundWorkerUseCase() : base("Outbound worker (start background processing)", 15, 1)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus(options => options.UseModel())
            .AddDbContextAbstraction<ExamplesDbContext>()
            .AddDbDistributedLockManager()
            .AddBroker<KafkaBroker>(options => options
                .AddDbOutboundConnector()
                .AddDbOutboundWorker(
                    interval: TimeSpan.FromMilliseconds(100),
                    distributedLockSettings: new DistributedLockSettings
                    {
                        AcquireRetryInterval = TimeSpan.FromSeconds(1)
                    }));

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider)
        {
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        ClientId = GetType().FullName
                    }
                }));

            _cancellationTokenSource = new CancellationTokenSource();

            Console.WriteLine("Starting OutboundWorker background process (press ESC to stop)...");

            var service = serviceProvider.GetRequiredService<IHostedService>();
            service.StartAsync(CancellationToken.None);
            _cancellationTokenSource.Token.Register(() => service.StopAsync(CancellationToken.None));
        }

        protected override Task Execute(IServiceProvider serviceProvider)
        {
            while (Console.ReadKey(false).Key != ConsoleKey.Escape)
            {
            }

            Console.WriteLine("Canceling...");

            _cancellationTokenSource.Cancel();

            // Let the worker gracefully exit
            Thread.Sleep(2000);

            return Task.CompletedTask;
        }
    }
}