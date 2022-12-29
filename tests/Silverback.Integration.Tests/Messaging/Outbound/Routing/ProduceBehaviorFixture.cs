// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing;

public class ProduceBehaviorFixture
{
    [Fact]
    public async Task HandleAsync_ShouldProduceWithConfiguredStrategy()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker());

        ProduceBehavior behavior = (ProduceBehavior)serviceProvider.GetServices<IBehavior>()
            .First(behavior => behavior is ProduceBehavior);

        TestProduceStrategy testProduceStrategy = new();
        OutboundEnvelope<TestEventOne> outboundEnvelope = new(
            new TestEventOne(),
            Array.Empty<MessageHeader>(),
            new TestProducerEndpointConfiguration("test")
            {
                Strategy = testProduceStrategy
            }.GetDefaultEndpoint(),
            Substitute.For<IProducer>());

        await behavior.HandleAsync(
            outboundEnvelope,
            message => ValueTask.FromResult(new[] { message }.AsReadOnlyCollection())!);
        await behavior.HandleAsync(
            outboundEnvelope,
            message => ValueTask.FromResult(new[] { message }.AsReadOnlyCollection())!);
        await behavior.HandleAsync(
            outboundEnvelope,
            message => ValueTask.FromResult(new[] { message }.AsReadOnlyCollection())!);

        testProduceStrategy.ProducedEnvelopes.Should().HaveCount(3);
        testProduceStrategy.ProducedEnvelopes.Select(envelope => envelope.Message)
            .Should().AllBeOfType<TestEventOne>();
    }

    private class TestProduceStrategy : IProduceStrategy
    {
        public List<IOutboundEnvelope> ProducedEnvelopes { get; } = new();

        public bool Equals(IProduceStrategy? other) => throw new NotSupportedException();

        public IProduceStrategyImplementation Build(IServiceProvider serviceProvider, ProducerEndpointConfiguration endpointConfiguration) =>
            new TestProduceStrategyImplementation(ProducedEnvelopes.Add);

        private class TestProduceStrategyImplementation : IProduceStrategyImplementation
        {
            private readonly Action<IOutboundEnvelope> _action;

            public TestProduceStrategyImplementation(Action<IOutboundEnvelope> action)
            {
                _action = action;
            }

            public Task ProduceAsync(IOutboundEnvelope envelope)
            {
                _action.Invoke(envelope);
                return Task.CompletedTask;
            }
        }
    }
}
