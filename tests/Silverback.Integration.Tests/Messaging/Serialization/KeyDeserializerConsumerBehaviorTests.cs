// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class KeyDeserializerConsumerBehaviorTests
{
    [Fact]
    public async Task HandleAsync_ShouldSetHeader_WhenRawKeyInEnvelopeAndDeserializerConfigured()
    {
        TestConsumerEndpoint endpoint = new(
            "test",
            TestConsumerEndpointConfiguration.GetDefault() with
            {
                KeyDeserializer = Utf8KeyDeserializer.Default
            });
        string key = "my-key";
        byte[]? rawKeyBytes = await GetRawKey(key);

        RawInboundEnvelope envelope = new(
            rawKey: rawKeyBytes,
            rawMessage: [],
            headers: [],
            endpoint: endpoint,
            consumer: Substitute.For<IConsumer>(),
            brokerMessageIdentifier: new TestOffset());

        await new KeyDeserializerConsumerBehavior().HandleAsync(
            new ConsumerPipelineContext(
                envelope,
                Substitute.For<IConsumer>(),
                Substitute.For<ISequenceStore>(),
                [],
                Substitute.For<IServiceProvider>()),
            (context, _) => default,
            CancellationToken.None);

        envelope.Headers.GetValue(DefaultMessageHeaders.MessageKey).ShouldBe(key);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSetKey_WhenNoDeserializerConfigured()
    {
        TestConsumerEndpoint endpoint = TestConsumerEndpoint.GetDefault();
        byte[]? rawKeyBytes = await GetRawKey("my-key");
        RawInboundEnvelope envelope = new(
            rawKey: rawKeyBytes,
            rawMessage: [],
            headers: [],
            endpoint: endpoint,
            consumer: Substitute.For<IConsumer>(),
            brokerMessageIdentifier: new TestOffset());

        await new KeyDeserializerConsumerBehavior().HandleAsync(
            new ConsumerPipelineContext(
                envelope,
                Substitute.For<IConsumer>(),
                Substitute.For<ISequenceStore>(),
                [],
                Substitute.For<IServiceProvider>()),
            (context, _) => default,
            CancellationToken.None);

        envelope.Headers.GetValue(DefaultMessageHeaders.MessageKey).ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSetKey_WhenRawKeyNotInEnvelope()
    {
        TestConsumerEndpoint endpoint = TestConsumerEndpoint.GetDefault();
        RawInboundEnvelope envelope = new(
            rawMessage: [],
            headers: [],
            endpoint: endpoint,
            consumer: Substitute.For<IConsumer>(),
            brokerMessageIdentifier: new TestOffset());

        await new KeyDeserializerConsumerBehavior().HandleAsync(
            new ConsumerPipelineContext(
                envelope,
                Substitute.For<IConsumer>(),
                Substitute.For<ISequenceStore>(),
                [],
                Substitute.For<IServiceProvider>()),
            (context, _) => default,
            CancellationToken.None);

        envelope.Headers.GetValue(DefaultMessageHeaders.MessageKey).ShouldBeNull();
    }

    private static async Task<byte[]?> GetRawKey(string key)
    {
        Stream? rawKeyStream = await Utf8KeySerializer.Default.SerializeAsync(key, [], TestProducerEndpoint.GetDefault());
        return await rawKeyStream.ReadAllAsync();
    }
}
