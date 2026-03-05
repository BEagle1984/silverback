// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class SchemaRegistryTests
{
    // lang=json
    private const string TestEventOneJsonSchema = """
                                                  {
                                                    "$schema": "http://json-schema.org/draft-04/schema#",
                                                    "title": "TestEventOne",
                                                    "type": "object",
                                                    "additionalProperties": false,
                                                    "properties": {
                                                      "ContentEventOne": {
                                                        "type": [
                                                          "null",
                                                          "string"
                                                        ]
                                                      }
                                                    }
                                                  }
                                                  """;

    // lang=json
    private static readonly Schema StringKeyJsonSchema = new(
        """
        {
          "$schema": "http://json-schema.org/draft-04/schema#",
          "title": "String",
          "type": "string"
        }
        """,
        SchemaType.Json);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SchemaRegistry_ShouldProduceAndConsumeJsonKeys(
        bool autoRegisterSchemas,
        bool normalizeSchemas)
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka().AddMockedConfluentSchemaRegistry())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .SerializeKeyAsJsonUsingSchemaRegistry(
                                            json => json
                                                .ConnectToSchemaRegistry("http://e2e:4242")
                                                .Configure(
                                                    config =>
                                                    {
                                                        config.NormalizeSchemas = normalizeSchemas;
                                                        config.AutoRegisterSchemas = autoRegisterSchemas;
                                                    }))
                                        .SerializeAsJsonUsingSchemaRegistry(
                                            json => json
                                                .ConnectToSchemaRegistry("http://e2e:4242")
                                                .Configure(
                                                    config =>
                                                    {
                                                        config.NormalizeSchemas = normalizeSchemas;
                                                        config.AutoRegisterSchemas = autoRegisterSchemas;
                                                    }))
                                        .ProduceTo(DefaultTopicName)
                                        .SetKafkaKey(x => x!.ContentEventOne)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .DeserializeJsonKeyUsingSchemaRegistry(
                                            json => json
                                                .ConnectToSchemaRegistry("http://e2e:4242"))
                                        .DeserializeJsonUsingSchemaRegistry(
                                            json => json
                                                .ConnectToSchemaRegistry("http://e2e:4242"))
                                        .ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        if (!autoRegisterSchemas)
        {
            await RegisterJsonSchemasAsync(normalizeSchemas);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(15);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(15);

        IEnumerable<string?> keys = Helper.Spy.InboundEnvelopes.Select(envelope => envelope.GetKafkaKey());
        keys.ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"), ignoreOrder: true);
    }

    private async Task RegisterJsonSchemasAsync(bool normalizeSchemas)
    {
        IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory = Host.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>();
        ISchemaRegistryClient schemaRegistryClient = schemaRegistryClientFactory.GetClient(registry => registry.WithUrl("http://e2e:4242"));
        await schemaRegistryClient.RegisterSchemaAsync(
            DefaultTopicName + "-key",
            StringKeyJsonSchema,
            normalizeSchemas);
        await schemaRegistryClient.RegisterSchemaAsync(
            DefaultTopicName + "-value",
            new Schema(TestEventOneJsonSchema, SchemaType.Json),
            normalizeSchemas);
    }
}
