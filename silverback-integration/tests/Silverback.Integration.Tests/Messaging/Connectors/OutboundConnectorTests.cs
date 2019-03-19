// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Connectors
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
            var endpoint = TestEndpoint.Default;

            var message = new TestEventOne { Content = "Test" };

            await _connector.RelayMessage(message, null, endpoint);

            _broker.ProducedMessages.Count.Should().Be(1);
            _broker.ProducedMessages.First().Endpoint.Should().Be(endpoint);

            var producedMessage = endpoint.Serializer.Deserialize(_broker.ProducedMessages.First().Message) as TestEventOne;
            producedMessage.Id.Should().Be(message.Id);
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_HeadersSent()
        {
            var endpoint = TestEndpoint.Default;

            var message = new TestEventOne { Content = "Test" };
            var headers = new[]
            {
                new MessageHeader("header1", "value1"),
                new MessageHeader("header2", "value2")
            };

            await _connector.RelayMessage(message, headers , endpoint);

            _broker.ProducedMessages.Count.Should().Be(1);
            _broker.ProducedMessages.First().Endpoint.Should().Be(endpoint);

            var producedMessage = _broker.ProducedMessages.First();
            producedMessage.Headers.Should().BeEquivalentTo(headers);
        }
    }
}