// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Silverback.Tests.Performance.TestTypes;
using Silverback.Util;

namespace Silverback.Tests.Performance.Broker
{
    internal sealed class ProduceStrategiesImplementation : IDisposable
    {
        private readonly IServiceProvider _rootServiceProvider;

        private readonly IServiceScope _serviceScope;

        public ProduceStrategiesImplementation()
        {
            _rootServiceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints

                            // Configure the properties needed by all consumers/producers
                            .Configure(
                                config =>
                                {
                                    // The bootstrap server address is needed to connect
                                    config.BootstrapServers =
                                        "PLAINTEXT://localhost:9092";
                                })

                            // Produce each SampleMessage to a samples-perf topic
                            .AddOutbound<SampleMessage1>(
                                endpoint => endpoint
                                    .ProduceTo("samples-perf-1"))
                            .AddOutbound<SampleMessage2>(
                                endpoint => endpoint
                                    .ProduceTo("samples-perf-2"))
                            .AddOutbound<SampleMessage3>(
                                endpoint => endpoint
                                    .ProduceTo("samples-perf-3"))
                            .AddOutbound<SampleMessage4>(
                                endpoint => endpoint
                                    .ProduceTo("samples-perf-4"))
                            .AddOutbound<SampleMessage4>(
                                endpoint => endpoint
                                    .ProduceTo("samples-perf-5"))
                            .AddOutbound<SampleMessage5>(
                                endpoint => endpoint
                                    .ProduceTo("samples-perf-6"))));

            _serviceScope = _rootServiceProvider.CreateScope();

            AsyncHelper.RunSynchronously(ConnectAsync);
        }

        public void Dispose()
        {
            AsyncHelper.RunSynchronously(DisconnectAsync);
            _serviceScope.Dispose();
        }

        // 1
        public async Task<Stats> RunPublishAsync(int iterations)
        {
            var publisher = _serviceScope.ServiceProvider.GetRequiredService<IPublisher>();

            int number = 0;
            var stats = new Stats("Publisher.PublishAsync");
            stats.StartProducing();

            while (number < iterations)
            {
                try
                {
                    await publisher.PublishAsync(
                        new SampleMessage1
                        {
                            Number = ++number
                        });

                    stats.IncrementProducedMessages();
                }
                catch (Exception)
                {
                    stats.IncrementErrors();
                }
            }

            stats.StopProducing();
            return stats;
        }

        // 2
        public async Task<Stats> RunProduceAsync(int iterations)
        {
            var producer = GetProducer("samples-perf-2");

            int number = 0;
            var stats = new Stats("Producer.ProduceAsync");
            stats.StartProducing();

            while (number < iterations)
            {
                try
                {
                    await producer.ProduceAsync(
                        new SampleMessage2
                        {
                            Number = ++number
                        });

                    stats.IncrementProducedMessages();
                }
                catch (Exception)
                {
                    stats.IncrementErrors();
                }
            }

            stats.StopProducing();
            return stats;
        }

        // 3
        public async Task<Stats> RunNoAwaitPublishAsync(int iterations)
        {
            var publisher = _serviceScope.ServiceProvider.GetRequiredService<IPublisher>();

            int number = 0;
            var stats = new Stats("Publisher.PublishAsync no await");
            stats.StartProducing();
            int pendingTasks = iterations;

            while (number < iterations)
            {
                publisher.PublishAsync(
                        new SampleMessage3
                        {
                            Number = ++number
                        })
                    .ContinueWith(
                        task =>
                        {
                            if (task.IsCompletedSuccessfully)
                                stats.IncrementProducedMessages();
                            else
                                stats.IncrementErrors();

                            Interlocked.Decrement(ref pendingTasks);
                        },
                        TaskScheduler.Default)
                    .FireAndForget();
            }

            while (pendingTasks > 0)
            {
                await Task.Delay(1);
            }

            stats.StopProducing();

            return stats;
        }

        // 4
        public async Task<Stats> RunNoAwaitProduceAsync(int iterations)
        {
            var producer = GetProducer("samples-perf-4");

            int number = 0;
            var stats = new Stats("Producer.ProduceAsync no await");
            stats.StartProducing();
            int pendingTasks = iterations;

            while (number < iterations)
            {
                producer.ProduceAsync(
                        new SampleMessage4
                        {
                            Number = ++number
                        })
                    .ContinueWith(
                        task =>
                        {
                            if (task.IsCompletedSuccessfully)
                                stats.IncrementProducedMessages();
                            else
                                stats.IncrementErrors();

                            Interlocked.Decrement(ref pendingTasks);
                        },
                        TaskScheduler.Default)
                    .FireAndForget();
            }

            while (pendingTasks > 0)
            {
                await Task.Delay(1);
            }

            stats.StopProducing();

            return stats;
        }

        // 5
        public async Task<Stats> RunProduceWithCallbacksAsync(int iterations)
        {
            var producer = GetProducer("samples-perf-5");

            int number = 0;
            var stats = new Stats("Producer.Produce with callbacks");
            stats.StartProducing();
            int pendingTasks = iterations;

            while (number < iterations)
            {
                producer.Produce(
                    new SampleMessage5
                    {
                        Number = ++number
                    },
                    null,
                    _ =>
                    {
                        stats.IncrementProducedMessages();
                        Interlocked.Decrement(ref pendingTasks);
                    },
                    _ =>
                    {
                        stats.IncrementErrors();
                        Interlocked.Decrement(ref pendingTasks);
                    });
            }

            while (pendingTasks > 0)
            {
                await Task.Delay(1);
            }

            stats.StopProducing();

            return stats;
        }

        // 6
        public async Task<Stats> RunWrappedProduceWithCallbacksAsync(int iterations)
        {
            var producer = GetProducer("samples-perf-6");

            int number = 0;
            var stats = new Stats("Producer.Produce with callbacks, wrapped in Task.Run");
            stats.StartProducing();
            int pendingTasks = iterations;

            while (number < iterations)
            {
                var nextNumber = ++number;
                Task.Run(
                        () =>
                        {
                            producer.Produce(
                                new SampleMessage6
                                {
                                    Number = nextNumber
                                },
                                null,
                                _ =>
                                {
                                    stats.IncrementProducedMessages();
                                    Interlocked.Decrement(ref pendingTasks);
                                },
                                _ =>
                                {
                                    stats.IncrementErrors();
                                    Interlocked.Decrement(ref pendingTasks);
                                });
                        })
                    .FireAndForget();
            }

            while (pendingTasks > 0)
            {
                await Task.Delay(1);
            }

            stats.StopProducing();

            return stats;
        }

        private async Task ConnectAsync()
        {
            Console.WriteLine("Connecting...");
            var broker = _rootServiceProvider.GetRequiredService<IBroker>();
            await broker.ConnectAsync();

            Console.WriteLine("Connected. Waiting 5 seconds...");

            // Wait and additional 5 seconds to ensure that the producer is
            // fully connected
            await Task.Delay(5000);
        }

        private async Task DisconnectAsync()
        {
            Console.WriteLine("Disconnecting...");
            var broker = _rootServiceProvider.GetRequiredService<IBroker>();
            await broker.DisconnectAsync();

            Console.WriteLine("Disconnected.");

            // Wait and additional 5 seconds to ensure that the producer is
            // fully connected
            await Task.Delay(5000);
        }

        private IProducer GetProducer(string endpointName) =>
            _serviceScope.ServiceProvider.GetRequiredService<KafkaBroker>()
                .Producers.First(producer => producer.Endpoint.Name == endpointName);

        internal class Stats
        {
            private readonly Stopwatch _stopwatch = new();

            private int _producedMessages;

            private int _errors;

            public Stats(string label)
            {
                Label = label;
            }

            public string Label { get; }

            public int ProducedMessages => _producedMessages;

            public int Errors => _errors;

            public TimeSpan Elapsed => _stopwatch.Elapsed;

            public void StartProducing() => _stopwatch.Start();

            public void StopProducing() => _stopwatch.Stop();

            public void IncrementProducedMessages() =>
                Interlocked.Increment(ref _producedMessages);

            public void IncrementErrors() =>
                Interlocked.Increment(ref _errors);
        }
    }
}
