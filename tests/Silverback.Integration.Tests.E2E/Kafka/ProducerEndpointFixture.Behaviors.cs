// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointFixture
{
    [Fact]
    public async Task ProducerEndpoint_ShouldForwardScopedServiceProviderToBehaviors()
    {
        List<Guid> receivedGuids = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<GuidWrapper>(_ => new GuidWrapper())
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddSingletonBrokerBehavior(new TestBehavior(receivedGuids))
            .AddTransientBrokerBehavior(_ => new TestBehavior(receivedGuids)));

        using IServiceScope scope1 = Host.ServiceProvider.CreateScope();
        Guid expected1 = scope1.ServiceProvider.GetRequiredService<GuidWrapper>().Value;

        IPublisher publisher1 = scope1.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher1.PublishEventAsync(new TestEventOne());

        DefaultTopic.MessagesCount.ShouldBe(1);
        receivedGuids.Count.ShouldBe(4);
        receivedGuids.ShouldAllBe(guid => guid == expected1);

        receivedGuids.Clear();

        using IServiceScope scope2 = Host.ServiceProvider.CreateScope();
        Guid expected2 = scope2.ServiceProvider.GetRequiredService<GuidWrapper>().Value;

        IPublisher publisher2 = scope2.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher2.PublishEventAsync(new TestEventOne());

        DefaultTopic.MessagesCount.ShouldBe(2);
        receivedGuids.Count.ShouldBe(4);
        receivedGuids.ShouldAllBe(guid => guid == expected2);

        expected2.ShouldNotBe(expected1);
    }

    private class GuidWrapper
    {
        public GuidWrapper(Guid? value = null)
        {
            Value = value ?? Guid.NewGuid();
        }

        public Guid Value { get; }
    }

    private class TestBehavior : IProducerBehavior
    {
        private readonly List<Guid> _guids;

        public TestBehavior(List<Guid> guids)
        {
            _guids = guids;
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer + 1;

        public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
        {
            _guids.Add(context.ServiceProvider.GetRequiredService<GuidWrapper>().Value);
            _guids.Add(context.Envelope.Context?.ServiceProvider.GetRequiredService<GuidWrapper>().Value ?? Guid.Empty);

            return next(context, cancellationToken);
        }
    }
}
