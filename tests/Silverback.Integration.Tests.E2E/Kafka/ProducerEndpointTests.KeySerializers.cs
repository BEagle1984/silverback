// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointTests
{
    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeKeyUsingSerializer_WhenKeySerializerConfigured()
    {
        TestKeySerializer keySerializer = new();
        byte[][] expectedKeys = ["100x"u8.ToArray(), "200x"u8.ToArray(), "300x"u8.ToArray()];
        await VerifyKeySerialization(
            endpoint => endpoint.SerializeKeyUsing(keySerializer),
            expectedKeys);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeKeyAsUtf8_WhenNoKeySerializerConfigured()
    {
        byte[][] expectedKeys = ["100"u8.ToArray(), "200"u8.ToArray(), "300"u8.ToArray()];
        await VerifyKeySerialization(_ => { }, expectedKeys);
    }

    private async Task VerifyKeySerialization(Action<KafkaProducerEndpointConfigurationBuilder<TestEventWithStringKafkaKey>> configureEndpoint, byte[][] expectedKeys)
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<TestEventWithStringKafkaKey>(endpoint =>
                        {
                            endpoint.ProduceTo("topic1");
                            configureEndpoint(endpoint);
                        }))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        for (int i = 1; i <= 3; i++)
        {
            string key = (i * 100).ToString(CultureInfo.InvariantCulture);
            await publisher.PublishAsync(new TestEventWithStringKafkaKey { Content = "Hello E2E!", KafkaKey = key });
        }

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        byte[]?[] keys = [.. topic1.GetAllMessages().ToList().Select(m => m.Key)];
        keys.ShouldBe(expectedKeys);
    }
}
