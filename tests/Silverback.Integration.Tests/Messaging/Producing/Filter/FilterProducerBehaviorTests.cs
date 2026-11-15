// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Filter;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Filter;

public class FilterProducerBehaviorTests
{
    private readonly LoggerSubstitute<IProducer> _loggerSubstitute;

    private readonly ISilverbackLogger<IProducer> _logger;

    public FilterProducerBehaviorTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddLoggerSubstitute(LogLevel.Trace)
            .AddSilverback()
            .WithConnectionToMessageBroker());

        _loggerSubstitute = (LoggerSubstitute<IProducer>)serviceProvider.GetRequiredService<ILogger<IProducer>>();
        _logger = serviceProvider.GetRequiredService<ISilverbackLogger<IProducer>>();
    }

    [Fact]
    public async Task HandleAsync_ShouldCallNext_WhenNoFilterIsConfigured()
    {
        ProducerPipelineContext context = CreateContext(null, new TestEventOne());
        bool nextCalled = false;

        await new FilterProducerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        nextCalled.ShouldBeTrue();
        _loggerSubstitute.DidNotReceive(LogLevel.Debug, null, null, 1007);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallNext_WhenFilterAcceptsMessage()
    {
        ProducerPipelineContext context = CreateContext(new TestFilter(), new TestEventOne { Content = "produce" });
        bool nextCalled = false;

        await new FilterProducerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        nextCalled.ShouldBeTrue();
        _loggerSubstitute.DidNotReceive(LogLevel.Debug, null, null, 1007);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCallNextAndLog_WhenFilterRejectsMessage()
    {
        ProducerPipelineContext context = CreateContext(new TestFilter(), new TestEventOne { Content = "discard" });
        bool nextCalled = false;

        await new FilterProducerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        nextCalled.ShouldBeFalse();
        _loggerSubstitute.Received(LogLevel.Debug, null, "Message filtered | EndpointName: test", 1007);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationTokenToFilter()
    {
        TestFilter filter = new();
        ProducerPipelineContext context = CreateContext(filter, new TestEventOne());
        using CancellationTokenSource cancellationTokenSource = new();

        await new FilterProducerBehavior(_logger).HandleAsync(context, (_, _) => default, cancellationTokenSource.Token);

        filter.CancellationTokens.ShouldBe([cancellationTokenSource.Token]);
    }

    [Theory]
    [InlineData("produce", true)]
    [InlineData("discard", false)]
    public async Task HandleAsync_ShouldResolveFilterFromServiceProvider(string content, bool shouldProduce)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services.AddSingleton<TestFilter>());
        ProducerPipelineContext context = CreateContext(
            new ServiceProviderOutboundMessageFilter(typeof(TestFilter)),
            new TestEventOne { Content = content },
            serviceProvider);
        bool nextCalled = false;

        await new FilterProducerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        nextCalled.ShouldBe(shouldProduce);
        serviceProvider.GetRequiredService<TestFilter>().Envelopes.ShouldBe([context.Envelope]);
    }

    [Fact]
    public async Task HandleAsync_ShouldResolveScopedFilterFromContextServiceProvider()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services.AddScoped<TestFilter>());
        using IServiceScope scope1 = serviceProvider.CreateScope();
        using IServiceScope scope2 = serviceProvider.CreateScope();
        ProducerPipelineContext context = CreateContext(
            new ServiceProviderOutboundMessageFilter(typeof(TestFilter)),
            new TestEventOne(),
            scope1.ServiceProvider);

        await new FilterProducerBehavior(_logger).HandleAsync(context, (_, _) => default, CancellationToken.None);

        scope1.ServiceProvider.GetRequiredService<TestFilter>().Envelopes.ShouldBe([context.Envelope]);
        scope2.ServiceProvider.GetRequiredService<TestFilter>().Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenFilterTypeIsNotRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(_ => { });
        ProducerPipelineContext context = CreateContext(
            new ServiceProviderOutboundMessageFilter(typeof(TestFilter)),
            new TestEventOne(),
            serviceProvider);
        bool nextCalled = false;

        Func<Task> act = () => new FilterProducerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None).AsTask();

        await act.ShouldThrowAsync<InvalidOperationException>();
        nextCalled.ShouldBeFalse();
    }

    private static ProducerPipelineContext CreateContext(
        IOutboundMessageFilter? filter,
        TestEventOne message,
        IServiceProvider? serviceProvider = null)
    {
        TestProducerEndpointConfiguration configuration = new("test")
        {
            Filter = filter
        };
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(configuration);
        OutboundEnvelope<TestEventOne> envelope = new(message, null, configuration, producer);

        return new ProducerPipelineContext(
            envelope,
            producer,
            [],
            (_, _) => ValueTask.CompletedTask,
            serviceProvider ?? Substitute.For<IServiceProvider>());
    }

    private sealed class TestFilter : IOutboundMessageFilter
    {
        public List<IOutboundEnvelope> Envelopes { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            CancellationTokens.Add(cancellationToken);

            return ValueTask.FromResult(envelope.Message is not TestEventOne { Content: "discard" });
        }
    }
}
