// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.RabbitMQ.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging.Behaviors
{
    public class RabbitRoutingKeyBehaviorTests
    {
        [Fact]
        public void Handle_NoRoutingKeyAttribute_KeyHeaderIsNotSet()
        {
            var message = new OutboundMessage<NoRoutingKeyMessage>(
                new NoRoutingKeyMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new RabbitExchangeProducerEndpoint("test-endpoint"));

            new RabbitRoutingKeyBehavior().Handle(new[] { message }, Task.FromResult);

            message.Headers.Should().NotContain(
                h => h.Key == "x-rabbit-routing-key");
        }

        [Fact]
        public void Handle_SingleRoutingKeyAttribute_KeyHeaderIsSet()
        {
            var message1 = new OutboundMessage<RoutingKeyMessage>(
                new RoutingKeyMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new RabbitExchangeProducerEndpoint("test-endpoint"));
            var message2 = new OutboundMessage<RoutingKeyMessage>(
                new RoutingKeyMessage
                {
                    Id = Guid.NewGuid(),
                    One = "a",
                    Two = "b",
                    Three = "c"
                },
                null,
                new RabbitExchangeProducerEndpoint("test-endpoint"));

            new RabbitRoutingKeyBehavior().Handle(new[] { message1, message2 }, Task.FromResult);

            message1.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-rabbit-routing-key", "1"));
            message2.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-rabbit-routing-key", "a"));
        }

        [Fact]
        public void Handle_MultipleRoutingKeyAttributes_KeyHeaderIsSet()
        {
            var message1 = new OutboundMessage<MultipleRoutingKeyAttributesMessage>(
                new MultipleRoutingKeyAttributesMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new RabbitExchangeProducerEndpoint("test-endpoint"));
            var message2 = new OutboundMessage<MultipleRoutingKeyAttributesMessage>(
                new MultipleRoutingKeyAttributesMessage
                {
                    Id = Guid.NewGuid(),
                    One = "a",
                    Two = "b",
                    Three = "c"
                },
                null,
                new RabbitExchangeProducerEndpoint("test-endpoint"));

            Func<Task> act = () => new RabbitRoutingKeyBehavior().Handle(new[] { message1, message2 }, Task.FromResult);

            act.Should().Throw<InvalidOperationException>();
        }
    }
}