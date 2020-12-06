// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing
{
    public class ProduceBehaviorTests
    {
        private readonly ProduceBehavior _behavior;

        private readonly InMemoryOutbox _outbox;

        private readonly TestBroker _broker;

        public ProduceBehaviorTests()
        {
            var services = new ServiceCollection();

            services.AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddOutbox<InMemoryOutbox>());

            services.AddNullLogger();

            var serviceProvider = services.BuildServiceProvider();

            _behavior = (ProduceBehavior)serviceProvider.GetServices<IBehavior>()
                .First(behavior => behavior is ProduceBehavior);
            _broker = serviceProvider.GetRequiredService<TestBroker>();
            _outbox = (InMemoryOutbox)serviceProvider.GetRequiredService<IOutboxWriter>();
        }

        [Fact]
        public async Task HandleAsync_OutboundMessage_RelayedToEndpoint()
        {
            var outboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                Array.Empty<MessageHeader>(),
                new TestProducerEndpoint("test"));

            await _behavior.HandleAsync(
                new[] { outboundEnvelope, outboundEnvelope, outboundEnvelope },
                Task.FromResult!);
            await _outbox.CommitAsync();

            var queued = await _outbox.ReadAsync(10);
            queued.Should().BeEmpty();
            _broker.ProducedMessages.Should().HaveCount(3);
        }

        [Fact]
        public async Task HandleAsync_OutboundMessageWithOutboxStrategy_RelayedToOutbox()
        {
            var outboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                Array.Empty<MessageHeader>(),
                new TestProducerEndpoint("test")
                {
                    Strategy = new OutboxProduceStrategy()
                });

            await _behavior.HandleAsync(
                new[] { outboundEnvelope, outboundEnvelope, outboundEnvelope },
                Task.FromResult!);
            await _outbox.CommitAsync();

            var queued = await _outbox.ReadAsync(10);
            queued.Should().HaveCount(3);
            _broker.ProducedMessages.Should().BeEmpty();
        }
    }
}
