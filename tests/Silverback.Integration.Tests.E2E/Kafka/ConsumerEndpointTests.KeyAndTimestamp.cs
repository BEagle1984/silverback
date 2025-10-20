// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointTests
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldSetKey()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(kafkaClientsConfigurationBuilder => kafkaClientsConfigurationBuilder
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { Content = "Hello E2E!", KafkaKey = i * 100 });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes[0].GetKafkaKey().ShouldBe("100");
        Helper.Spy.InboundEnvelopes[1].GetKafkaKey().ShouldBe("200");
        Helper.Spy.InboundEnvelopes[2].GetKafkaKey().ShouldBe("300");
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldSetTimestamp()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(kafkaClientsConfigurationBuilder => kafkaClientsConfigurationBuilder
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope envelope = Helper.Spy.InboundEnvelopes.Single();
        envelope.GetKafkaTimestamp().ShouldBeLessThan(DateTime.Now);
        envelope.GetKafkaTimestamp().ShouldBeGreaterThan(DateTime.Now.AddSeconds(-1));
    }
}
