// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound
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
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>());

            var serviceProvider = services.BuildServiceProvider();

            _broker = serviceProvider.GetRequiredService<TestBroker>();
            _connector = new OutboundConnector(new BrokerCollection(new[] { _broker }));
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_Relayed()
        {
            var envelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne { Content = "Test" },
                null,
                TestProducerEndpoint.GetDefault());

            await _connector.RelayMessage(envelope);

            _broker.ProducedMessages.Should().HaveCount(1);
            _broker.ProducedMessages.First().Endpoint.Should().Be(envelope.Endpoint);

            var producedMessage = (await envelope.Endpoint.Serializer.DeserializeAsync(
                _broker.ProducedMessages.First().Message,
                new MessageHeaderCollection(_broker.ProducedMessages.First().Headers),
                MessageSerializationContext.Empty)).Item1 as TestEventOne;
            producedMessage.Should().BeEquivalentTo(envelope.Message);
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_HeadersSent()
        {
            var message = new TestEventOne { Content = "Test" };
            var headers = new[]
            {
                new MessageHeader("header1", "value1"),
                new MessageHeader("header2", "value2")
            };
            var envelope = new OutboundEnvelope<TestEventOne>(
                message,
                headers,
                TestProducerEndpoint.GetDefault());

            await _connector.RelayMessage(envelope);

            _broker.ProducedMessages.Should().HaveCount(1);
            _broker.ProducedMessages.First().Endpoint.Should().Be(envelope.Endpoint);

            var producedMessage = _broker.ProducedMessages.First();
            producedMessage.Headers.Should()
                .ContainEquivalentOf(new MessageHeader("header1", "value1"))
                .And.ContainEquivalentOf(new MessageHeader("header2", "value2"));
        }
    }
}
