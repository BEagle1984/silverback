// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Deferred
{
    public sealed class OutboundWorkerUseCase : UseCase, IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource;

        public OutboundWorkerUseCase()
        {
            Title = "Start outbound worker";
            Description = "The outbound worker monitors the outbox table and publishes the messages to Kafka.";
            ExecutionsCount = 1;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .UseDbContext<ExamplesDbContext>()
                .AddDbDistributedLockManager()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka()
                        .AddDbOutboundConnector()
                        .AddDbOutboundWorker(
                            new DistributedLockSettings(
                                acquireRetryInterval: TimeSpan.FromSeconds(1),
                                heartbeatTimeout: TimeSpan.FromSeconds(10),
                                heartbeatInterval: TimeSpan.FromSeconds(1))));
        }

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider)
        {
            configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("silverback-examples-events")
                        {
                            Configuration = new KafkaProducerConfig
                            {
                                BootstrapServers = "PLAINTEXT://localhost:9092"
                            }
                        }));

            _cancellationTokenSource = new CancellationTokenSource();

            Console.WriteLine("Starting OutboundWorker background process (press CTRL-C to stop)...");

            var service = serviceProvider.GetRequiredService<IHostedService>();
            service.StartAsync(CancellationToken.None);
            _cancellationTokenSource.Token.Register(() => service.StopAsync(CancellationToken.None));
        }

        protected override Task Execute(IServiceProvider serviceProvider)
        {
            WorkerHelper.LoopUntilCancelled();

            Console.WriteLine("Canceling...");

            // Let the worker gracefully exit
            Thread.Sleep(2000);

            return Task.CompletedTask;
        }
    }
}
