// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Protocol;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesTests
{
    [Fact]
    public async Task SkipPolicy_ShouldSkipMessage_WhenJsonDeserializationFails()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip()))))
            .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldBeEmpty();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task SkipPolicy_ShouldSkipMessage_WhenOtherEndpointIsStreaming()
    {
        int streamsCount = 0;
        TestingCollection<string?> streamedMessages = [];
        TestingCollection<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom("e2e/stream")
                        .AllowStreaming())
                    .Consume<TestEventTwo>(endpoint => endpoint
                        .ConsumeFrom("e2e/skip")
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<TestEventTwo>(HandleMessage));

        void HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref streamsCount);

            foreach (TestEventOne message in stream)
            {
                streamedMessages.Add(message.ContentEventOne);
            }
        }

        void HandleMessage(TestEventTwo message)
        {
            if (message.ContentEventTwo == "fail")
                throw new InvalidOperationException("Test");

            receivedMessages.Add(message.ContentEventTwo);
        }

        IProducer streamProducer = Helper.GetProducerForEndpoint("e2e/stream");
        IProducer skipProducer = Helper.GetProducerForEndpoint("e2e/skip");

        await streamProducer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await AsyncTestingUtil.WaitAsync(() => streamedMessages.Count > 0);

        await skipProducer.ProduceAsync(new TestEventTwo { ContentEventTwo = "fail" });
        await skipProducer.ProduceAsync(new TestEventTwo { ContentEventTwo = "2" });
        await streamProducer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        streamsCount.ShouldBe(1);
        streamedMessages.ShouldBe(["1", "3"]);
        receivedMessages.ShouldBe(["2"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task SkipPolicy_ShouldSkipAndAcknowledgeMessage_WhenOtherEndpointBatchIsPending(
        MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        TestingCollection<List<string?>> receivedBatches = [];
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>()).Returns(Task.CompletedTask);

        // The endpoints of a client share the batch: only a message failing before being added to it (e.g. not deserializable) is
        // skipped while the batch is pending
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom("e2e/batch")
                        .WithQualityOfServiceLevel(qualityOfServiceLevel)
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5)))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom("e2e/skip")
                        .WithQualityOfServiceLevel(qualityOfServiceLevel)
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IProducer batchProducer = Helper.GetProducerForEndpoint("e2e/batch");
        IProducer skipProducer = Helper.GetProducerForEndpoint("e2e/skip");

        await batchProducer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count == 1);

        await skipProducer.RawProduceAsync("not json"u8.ToArray());

        // The skipped message is acknowledged right away, while the batch is pending
        await AsyncTestingUtil.WaitAsync(() => callback.ReceivedCalls().Any() && DefaultClientSession.GetPendingMessagesCount("e2e/skip") == 0);
        await callback.Received(1).OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>());
        DefaultClientSession.GetPendingMessagesCount("e2e/skip").ShouldBe(0);
        DefaultClientSession.GetPendingMessagesCount("e2e/batch").ShouldBe(1);

        await skipProducer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await batchProducer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.ShouldHaveSingleItem().ShouldBe(["1", "2", "3"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task SkipPolicy_ShouldSkipAndAcknowledgeMessage_WhenOtherEndpointChunkedMessageIsIncomplete()
    {
        TestingCollection<string?> receivedChunkedMessages = [];
        TestingCollection<string?> receivedMessages = [];
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>()).Returns(Task.CompletedTask);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom("e2e/chunked")
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventTwo>(endpoint => endpoint
                        .ConsumeFrom("e2e/skip")
                        .WithAtLeastOnceQoS()
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<TestEventOne>(message => receivedChunkedMessages.Add(message.ContentEventOne))
            .AddDelegateSubscriber<TestEventTwo>(message => receivedMessages.Add(message.ContentEventTwo))
            .AddSingletonErrorPolicyCallback(callback));

        IProducer chunkedProducer = Helper.GetProducerForEndpoint("e2e/chunked");
        IProducer skipProducer = Helper.GetProducerForEndpoint("e2e/skip");
        byte[][] chunks = ChunksHelper.SerializeAndSplitIntoChunks("Long message", 2);

        await chunkedProducer.RawProduceAsync(chunks[0], GetRawChunkHeaders("chunked", 0, chunks.Length));
        await AsyncTestingUtil.WaitAsync(() => DefaultClientSession.GetPendingMessagesCount("e2e/chunked") == 1);

        // Not deserializable: skipped before reaching the sequencer, which would abort the incomplete chunked message
        await skipProducer.RawProduceAsync("not json"u8.ToArray());

        // The skipped message is acknowledged right away, while the chunked message is incomplete
        await AsyncTestingUtil.WaitAsync(() => callback.ReceivedCalls().Any() && DefaultClientSession.GetPendingMessagesCount("e2e/skip") == 0);
        await callback.Received(1).OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>());
        DefaultClientSession.GetPendingMessagesCount("e2e/skip").ShouldBe(0);
        DefaultClientSession.GetPendingMessagesCount("e2e/chunked").ShouldBe(1);

        await chunkedProducer.RawProduceAsync(chunks[1], GetRawChunkHeaders("chunked", 1, chunks.Length));
        await skipProducer.ProduceAsync(new TestEventTwo { ContentEventTwo = "2" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedChunkedMessages.ShouldBe(["Long message"]);
        receivedMessages.ShouldBe(["2"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    private static MessageHeader[] GetRawChunkHeaders(string chunkMessageId, int chunkIndex, int chunksCount) =>
    [
        new(MqttMessageHeaders.ChunkMessageId, chunkMessageId),
        new(DefaultMessageHeaders.ChunkIndex, chunkIndex.ToString(CultureInfo.InvariantCulture)),
        new(DefaultMessageHeaders.ChunksCount, chunksCount.ToString(CultureInfo.InvariantCulture))
    ];
}
