// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Serialization;

public class SchemaRegistryMessageSerializerTests
{
    private readonly ISchemaRegistryClient _schemaRegistryClient = Substitute.For<ISchemaRegistryClient>();

    [Fact]
    public async Task SerializeAsync_ShouldReturnStreamWhoseBufferCanBeProducedWithoutCopying()
    {
        _schemaRegistryClient
            .RegisterSchemaWithResponseAsync(Arg.Any<string>(), Arg.Any<Schema>(), Arg.Any<bool>())
            .Returns(call => new RegisteredSchema(call.Arg<string>(), 1, 1, call.Arg<Schema>().SchemaString, SchemaType.Json, []));
        JsonSchemaRegistryMessageSerializer<TestEventOne> serializer = new(
            _schemaRegistryClient,
            new JsonSerializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic });

        Stream? serialized = await serializer.SerializeAsync(
            new TestEventOne { Content = "the message" },
            [],
            TestProducerEndpoint.GetDefault());

        byte[]? producedBytes = serialized.ReadAll();
        producedBytes.ShouldNotBeNull();
        producedBytes.ShouldNotBeEmpty();
        serialized.ReadAll().ShouldBeSameAs(producedBytes);
    }

    [Fact]
    public async Task SerializeAsync_ShouldNotShareBufferReturnedByCustomSerializer()
    {
        byte[] buffer = [0x01, 0x02, 0x03];
        IAsyncSerializer<TestEventOne> confluentSerializer = Substitute.For<IAsyncSerializer<TestEventOne>>();
        confluentSerializer.SerializeAsync(Arg.Any<TestEventOne>(), Arg.Any<SerializationContext>()).Returns(buffer);
        CustomSerializer serializer = new(_schemaRegistryClient, confluentSerializer);

        Stream? serialized = await serializer.SerializeAsync(new TestEventOne(), [], TestProducerEndpoint.GetDefault());

        byte[]? producedBytes = serialized.ReadAll();
        producedBytes.ShouldBe(buffer);
        producedBytes.ShouldNotBeSameAs(buffer);
    }

    [Fact]
    public async Task SerializeAsync_ShouldNotShareByteArray()
    {
        byte[] messageBytes = [0x01, 0x02, 0x03];
        JsonSchemaRegistryMessageSerializer<TestEventOne> serializer = new(_schemaRegistryClient);

        Stream? serialized = await serializer.SerializeAsync(messageBytes, [], TestProducerEndpoint.GetDefault());

        byte[]? producedBytes = serialized.ReadAll();
        producedBytes.ShouldBe(messageBytes);
        producedBytes.ShouldNotBeSameAs(messageBytes);
    }

    private sealed class CustomSerializer : SchemaRegistryMessageSerializer<TestEventOne>
    {
        public CustomSerializer(ISchemaRegistryClient schemaRegistryClient, IAsyncSerializer<TestEventOne> confluentSerializer)
            : base(schemaRegistryClient, confluentSerializer)
        {
        }
    }
}
