// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointTests
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldDeserializeKeyUsingDeserializer_WhenKeyDeserializerConfigured()
    {
        static void Configure(KafkaConsumerEndpointConfigurationBuilder<object> endpoint) => endpoint.DeserializeKeyUsing(new TestKeyDeserializer());

        await VerifyKeyDeserialization(Configure, ["100x", "200x", "300x"]);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldDeserializeKeyFromUtf8_WhenNoKeySerializerConfigured()
        => await VerifyKeyDeserialization(_ => { }, ["100", "200", "300"]);

    private async Task VerifyKeyDeserialization(Action<KafkaConsumerEndpointConfigurationBuilder<object>> configureEndpoint, string[] expectedKeys)
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint =>
                                    {
                                        endpoint.ConsumeFrom(DefaultTopicName);
                                        configureEndpoint(endpoint);
                                    })))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            string key = (i * 100).ToString(CultureInfo.InvariantCulture);
            await producer.ProduceAsync(new TestEventWithStringKafkaKey { Content = "Hello E2E!", KafkaKey = key });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        List<string?> keys = [.. Helper.Spy.InboundEnvelopes.Select(e => e.GetKafkaKey())];

        keys.ShouldBe(expectedKeys);
    }
}
