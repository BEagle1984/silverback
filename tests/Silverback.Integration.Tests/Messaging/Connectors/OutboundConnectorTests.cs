// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    public class OutboundConnectorTests
    {
        private OutboundConnector _connector;
        private TestBroker _broker;

        public OutboundConnectorTests()
        {
            _broker = new TestBroker();
            _connector = new OutboundConnector(_broker);
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_Relayed()
        {
            var outboundMessage = new OutboundMessage<TestEventOne>(new TestEventOne { Content = "Test" }, null, TestEndpoint.Default);

            await _connector.RelayMessage(outboundMessage);

            _broker.ProducedMessages.Count.Should().Be(1);
            _broker.ProducedMessages.First().Endpoint.Should().Be(outboundMessage.Endpoint);

            var producedMessage = outboundMessage.Endpoint.Serializer.Deserialize(
                _broker.ProducedMessages.First().Message, 
                new MessageHeaderCollection(_broker.ProducedMessages.First().Headers)) as TestEventOne;
            producedMessage.Id.Should().Be(outboundMessage.Content.Id);
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_HeadersSent()
        {
            var outboundMessage = new OutboundMessage<TestEventOne>(
                new TestEventOne { Content = "Test" },
                new[]
                {
                    new MessageHeader("header1", "value1"),
                    new MessageHeader("header2", "value2")
                },
                TestEndpoint.Default);

            await _connector.RelayMessage(outboundMessage);

            _broker.ProducedMessages.Count.Should().Be(1);
            _broker.ProducedMessages.First().Endpoint.Should().Be(outboundMessage.Endpoint);

            var producedMessage = _broker.ProducedMessages.First();
            producedMessage.Headers.Should().Contain(outboundMessage.Headers);
        }
    }
}