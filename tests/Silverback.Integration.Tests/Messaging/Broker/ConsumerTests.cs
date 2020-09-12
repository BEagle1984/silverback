// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker
{
    public class ConsumerTests
    {
        private readonly TestBroker _broker;

        public ConsumerTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

            var serviceProvider = services.BuildServiceProvider();

            _broker = serviceProvider.GetRequiredService<TestBroker>();
        }

        // TODO: Migrate or delete tests

        // [Fact]
        // public async Task HandleMessage_SomeMessages_MessagesReceived()
        // {
        //     var envelopes = new List<IRawInboundEnvelope>();
        //     var consumer = (TestConsumer)_broker.AddConsumer(
        //         TestConsumerEndpoint.GetDefault(),
        //         args => envelopes.AddRange(args.Envelopes));
        //     _broker.Connect();
        //
        //     await consumer.TestHandleMessage(new TestEventOne());
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //     await consumer.TestHandleMessage(new TestEventOne());
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //
        //     envelopes.Count.Should().Be(5);
        // }
        //
        // [Fact]
        // public async Task HandleMessage_MessageCorrectlyProcessed_ConsumingCompletedEventPublished()
        // {
        //     var consumer = (TestConsumer)_broker.AddConsumer(
        //         TestConsumerEndpoint.GetDefault(),
        //         args =>
        //         {
        //             /* processing */
        //         });
        //     _broker.Connect();
        //
        //     await consumer.TestHandleMessage(new TestEventOne());
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //     await consumer.TestHandleMessage(new TestEventOne());
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //
        //     _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingCompletedEvent>()
        //         .Count().Should().Be(5);
        // }
        //
        // [Fact]
        // public async Task HandleMessage_SomeMessagesFailToBeProcessed_ConsumingCompletedAndAbortedEventsPublished()
        // {
        //     var consumer = (TestConsumer)_broker.AddConsumer(
        //         TestConsumerEndpoint.GetDefault(),
        //         args =>
        //         {
        //             if (args.Envelopes.First() is IInboundEnvelope<TestEventOne>)
        //                 throw new InvalidOperationException();
        //         });
        //     _broker.Connect();
        //
        //     try
        //     {
        //         await consumer.TestHandleMessage(new TestEventOne());
        //     }
        //     catch
        //     {
        //         // ignored
        //     }
        //
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //
        //     try
        //     {
        //         await consumer.TestHandleMessage(new TestEventOne());
        //     }
        //     catch
        //     {
        //         // ignored
        //     }
        //
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //     await consumer.TestHandleMessage(new TestEventTwo());
        //
        //     _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingCompletedEvent>()
        //         .Count().Should().Be(3);
        //     _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingAbortedEvent>()
        //         .Count().Should().Be(2);
        // }
        //
        // [Fact]
        // public async Task HandleMessage_SomeMessages_HeadersReceivedWithInboundMessages()
        // {
        //     var envelopes = new List<IRawInboundEnvelope>();
        //     var consumer = (TestConsumer)_broker.AddConsumer(
        //         TestConsumerEndpoint.GetDefault(),
        //         args => envelopes.AddRange(args.Envelopes));
        //     _broker.Connect();
        //
        //     await consumer.TestHandleMessage(
        //         new TestEventOne(),
        //         new[] { new MessageHeader("name", "value1") });
        //     await consumer.TestHandleMessage(
        //         new TestEventOne(),
        //         new[] { new MessageHeader("name", "value2") });
        //
        //     var firstMessage = envelopes.First();
        //     firstMessage.Headers.Count.Should().Be(2);
        //     firstMessage.Headers.Select(h => h.Name).Should().BeEquivalentTo("name", "x-message-type");
        //     firstMessage.Headers.GetValue("name").Should().Be("value1");
        //     var secondMessage = envelopes.Skip(1).First();
        //     secondMessage.Headers.Count.Should().Be(2);
        //     secondMessage.Headers.Select(h => h.Name).Should().BeEquivalentTo("name", "x-message-type");
        //     secondMessage.Headers.GetValue("name").Should().Be("value2");
        // }
        //
        // [Fact]
        // public async Task HandleMessage_SomeMessages_FailedAttemptsReceivedWithInboundMessages()
        // {
        //     var envelopes = new List<IRawInboundEnvelope>();
        //     var consumer = (TestConsumer)_broker.AddConsumer(
        //         TestConsumerEndpoint.GetDefault(),
        //         args => envelopes.AddRange(args.Envelopes));
        //     _broker.Connect();
        //
        //     await consumer.TestHandleMessage(new TestEventOne());
        //     await consumer.TestHandleMessage(
        //         new TestEventOne(),
        //         new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3") });
        //
        //     envelopes.First().Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts).Should().Be(null);
        //     envelopes.Skip(1).First().Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts).Should().Be(3);
        // }

        [Fact]
        public void StatusInfo_BeforeConnect_StatusIsDisconnected()
        {
            var consumer = (TestConsumer)_broker.AddConsumer(TestConsumerEndpoint.GetDefault());

            consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Disconnected);
        }

        [Fact]
        public void StatusInfo_AfterConnect_StatusIsConnected()
        {
            var consumer = (TestConsumer)_broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);
        }

        [Fact]
        public void StatusInfo_AfterConnect_StatusHistoryRecorded()
        {
            var consumer = (TestConsumer)_broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            consumer.StatusInfo.History.Count.Should().Be(1);
            consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Connected);
            consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();
        }

        [Fact]
        public void StatusInfo_AfterDisconnect_StatusIsDisconnected()
        {
            var consumer = (TestConsumer)_broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            _broker.Connect();
            _broker.Disconnect();

            consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Disconnected);
        }

        [Fact]
        public void StatusInfo_AfterDisconnect_StatusHistoryRecorded()
        {
            var consumer = (TestConsumer)_broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            _broker.Connect();
            _broker.Disconnect();

            consumer.StatusInfo.History.Count.Should().Be(2);
            consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Disconnected);
            consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();
        }

        [Fact]
        public async Task StatusInfo_AfterConsuming_StatusIsConsuming()
        {
            var consumer = (TestConsumer)_broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne());

            consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);
        }

        [Fact]
        public async Task StatusInfo_AfterConsuming_StatusHistoryRecorded()
        {
            var consumer = (TestConsumer)_broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne());
            await consumer.TestHandleMessage(new TestEventOne());

            consumer.StatusInfo.History.Count.Should().Be(2);
            consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Consuming);
            consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();
        }

        [Fact]
        public async Task StatusInfo_AfterConsuming_LastConsumedMessageTracked()
        {
            var consumer = (TestConsumer)_broker.AddConsumer(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne(), null, new TestOffset("test", "1"));
            await consumer.TestHandleMessage(new TestEventOne(), null, new TestOffset("test", "2"));

            consumer.StatusInfo.ConsumedMessagesCount.Should().Be(2);
            consumer.StatusInfo.LastConsumedMessageOffset.Should().BeEquivalentTo(new TestOffset("test", "2"));
            consumer.StatusInfo.LastConsumedMessageTimestamp.Should().NotBeNull();
        }
    }
}
