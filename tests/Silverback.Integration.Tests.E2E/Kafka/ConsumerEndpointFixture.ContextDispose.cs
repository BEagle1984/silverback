// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointFixture
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldDisposeScopedServices()
    {
        ConcurrentBag<ScopedService> scopedServices = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<ScopedService>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne, ScopedService>(HandleMessage));

        void HandleMessage(TestEventOne message, ScopedService scopedService) => scopedServices.Add(scopedService);

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        scopedServices.Count.ShouldBe(5);
        scopedServices.ShouldAllBe(service => service.Disposed);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldDisposeScopedServices_WhenBatchProcessing()
    {
        ConcurrentBag<ScopedService> scopedServices = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<ScopedService>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(2))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, ScopedService>(HandleMessageAsync));

        async ValueTask HandleMessageAsync(IAsyncEnumerable<TestEventOne> messages, ScopedService scopedService)
        {
            await messages.ToListAsync();
            scopedServices.Add(scopedService);

            // Ensure the service is not prematurely disposed
            if (scopedService.Disposed)
                throw new InvalidOperationException("Service is disposed.");
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 6; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        scopedServices.Count.ShouldBe(3);
        scopedServices.ShouldAllBe(service => service.Disposed);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldDisposeScopedServices_WhenBatchProcessingWithTimeout()
    {
        ConcurrentBag<ScopedService> scopedServices = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<ScopedService>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(5, TimeSpan.FromMilliseconds(100)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, ScopedService>(HandleMessageAsync));

        async ValueTask HandleMessageAsync(IAsyncEnumerable<TestEventOne> messages, ScopedService scopedService)
        {
            await messages.ToListAsync();
            scopedServices.Add(scopedService);

            // Ensure the service is not prematurely disposed
            if (scopedService.Disposed)
                throw new InvalidOperationException("Service is disposed.");
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        scopedServices.Count.ShouldBe(1);
        scopedServices.ShouldAllBe(service => service.Disposed);
    }

    private class ScopedService : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}
