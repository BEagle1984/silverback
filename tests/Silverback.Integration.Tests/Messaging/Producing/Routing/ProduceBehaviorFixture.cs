// Copyright (c) 2024 Sergio Aquilini
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

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

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
            [],
            new TestProducerEndpointConfiguration("test")
            {
                Strategy = testProduceStrategy
            }.GetDefaultEndpoint(),
            Substitute.For<IProducer>(),
            new SilverbackContext());

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

    [Fact]
    public async Task HandleAsync_ShouldProduceEnumerable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker());

        ProduceBehavior behavior = (ProduceBehavior)serviceProvider.GetServices<IBehavior>()
            .First(behavior => behavior is ProduceBehavior);

        IEnumerable<TestEventOne> messages =
        [
            new TestEventOne(),
            new TestEventOne(),
            new TestEventOne()
        ];

        TestProduceStrategy testProduceStrategy = new();
        OutboundEnvelope<IEnumerable<TestEventOne>> outboundEnvelope = new(
            messages,
            [],
            new TestProducerEndpointConfiguration("test")
            {
                Strategy = testProduceStrategy
            }.GetDefaultEndpoint(),
            Substitute.For<IProducer>(),
            new SilverbackContext());

        await behavior.HandleAsync(
            outboundEnvelope,
            message => ValueTask.FromResult(new[] { message }.AsReadOnlyCollection())!);

        testProduceStrategy.ProducedEnvelopes.Should().HaveCount(3);
        testProduceStrategy.ProducedEnvelopes.Select(envelope => envelope.Message)
            .Should().AllBeOfType<TestEventOne>();
    }

    [Fact]
    public async Task HandleAsync_ShouldProduceAsyncEnumerable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker());

        ProduceBehavior behavior = (ProduceBehavior)serviceProvider.GetServices<IBehavior>()
            .First(behavior => behavior is ProduceBehavior);

        IAsyncEnumerable<TestEventOne> messages = new[]
        {
            new TestEventOne(),
            new TestEventOne(),
            new TestEventOne()
        }.ToAsyncEnumerable();

        TestProduceStrategy testProduceStrategy = new();
        OutboundEnvelope<IAsyncEnumerable<TestEventOne>> outboundEnvelope = new(
            messages,
            [],
            new TestProducerEndpointConfiguration("test")
            {
                Strategy = testProduceStrategy
            }.GetDefaultEndpoint(),
            Substitute.For<IProducer>(),
            new SilverbackContext());

        await behavior.HandleAsync(
            outboundEnvelope,
            message => ValueTask.FromResult(new[] { message }.AsReadOnlyCollection())!);

        testProduceStrategy.ProducedEnvelopes.Should().HaveCount(3);
        testProduceStrategy.ProducedEnvelopes.Select(envelope => envelope.Message)
            .Should().AllBeOfType<TestEventOne>();
    }

    private class TestProduceStrategy : IProduceStrategy
    {
        public List<IOutboundEnvelope> ProducedEnvelopes { get; } = [];

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

            public Task ProduceAsync(IEnumerable<IOutboundEnvelope> envelopes)
            {
                envelopes.ForEach(_action.Invoke);
                return Task.CompletedTask;
            }

            public Task ProduceAsync(IAsyncEnumerable<IOutboundEnvelope> envelopes) => envelopes.ForEachAsync(_action.Invoke);
        }
    }
}
