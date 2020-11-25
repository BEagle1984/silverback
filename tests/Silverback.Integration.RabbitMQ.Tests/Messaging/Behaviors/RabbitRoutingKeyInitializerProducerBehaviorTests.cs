// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.RabbitMQ.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging.Behaviors
{
    public class RabbitRoutingKeyInitializerProducerBehaviorTests
    {
        [Fact]
        public void HandleAsync_NoRoutingKeyAttribute_KeyHeaderIsNotSet()
        {
            var envelope = new OutboundEnvelope<NoRoutingKeyMessage>(
                new NoRoutingKeyMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new RabbitExchangeProducerEndpoint("test-endpoint"));

            new RabbitRoutingKeyInitializerProducerBehavior().HandleAsync(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
                _ => Task.CompletedTask);

            envelope.Headers.Should().NotContain(h => h.Name == "x-rabbit-routing-key");
        }

        [Fact]
        public void HandleAsync_SingleRoutingKeyAttribute_KeyHeaderIsSet()
        {
            var envelope = new OutboundEnvelope<RoutingKeyMessage>(
                new RoutingKeyMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new RabbitExchangeProducerEndpoint("test-endpoint"));

            new RabbitRoutingKeyInitializerProducerBehavior().HandleAsync(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
                _ => Task.CompletedTask);

            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-rabbit-routing-key", "1"));
        }

        [Fact]
        public void HandleAsync_MultipleRoutingKeyAttributes_KeyHeaderIsSet()
        {
            var envelope = new OutboundEnvelope<MultipleRoutingKeyAttributesMessage>(
                new MultipleRoutingKeyAttributesMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new RabbitExchangeProducerEndpoint("test-endpoint"));

            Func<Task> act = () =>
                new RabbitRoutingKeyInitializerProducerBehavior().HandleAsync(
                    new ProducerPipelineContext(
                        envelope,
                        Substitute.For<IProducer>(),
                        Substitute.For<IServiceProvider>()),
                    _ => Task.CompletedTask);

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
