// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    public class OutboundConnectorTests
    {
        private readonly OutboundConnector _connector;
        private readonly TestBroker _broker;

        public OutboundConnectorTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>());

            var serviceProvider = services.BuildServiceProvider();

            _broker = serviceProvider.GetRequiredService<TestBroker>();
            _connector = new OutboundConnector(new BrokerCollection(new[] { _broker }));
        }

        [Fact]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task OnMessageReceived_SingleMessage_Relayed()
        {
            var envelope = new OutboundEnvelope<TestEventOne>(new TestEventOne { Content = "Test" }, null,
                TestProducerEndpoint.GetDefault());

            await _connector.RelayMessage(envelope);

            _broker.ProducedMessages.Count.Should().Be(1);
            _broker.ProducedMessages.First().Endpoint.Should().Be(envelope.Endpoint);

            var producedMessage = envelope.Endpoint.Serializer.Deserialize(
                _broker.ProducedMessages.First().Message,
                new MessageHeaderCollection(_broker.ProducedMessages.First().Headers),
                MessageSerializationContext.Empty) as TestEventOne;
            producedMessage.Should().BeEquivalentTo(envelope.Message);
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_HeadersSent()
        {
            var envelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne { Content = "Test" },
                new[]
                {
                    new MessageHeader("header1", "value1"),
                    new MessageHeader("header2", "value2")
                },
                TestProducerEndpoint.GetDefault());

            await _connector.RelayMessage(envelope);

            _broker.ProducedMessages.Count.Should().Be(1);
            _broker.ProducedMessages.First().Endpoint.Should().Be(envelope.Endpoint);

            var producedMessage = _broker.ProducedMessages.First();
            producedMessage.Headers.Should()
                .ContainEquivalentOf(new MessageHeader("header1", "value1"))
                .And.ContainEquivalentOf(new MessageHeader("header2", "value2"));
        }
    }
}