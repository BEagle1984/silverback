// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
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
using Silverback.Messaging.Consuming.Filter;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.Filter;

public class FilterConsumerBehaviorTests
{
    private readonly LoggerSubstitute<FilterConsumerBehavior> _loggerSubstitute;

    private readonly ISilverbackLogger<FilterConsumerBehavior> _logger;

    public FilterConsumerBehaviorTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddLoggerSubstitute(LogLevel.Trace)
            .AddSilverback()
            .WithConnectionToMessageBroker());

        _loggerSubstitute = (LoggerSubstitute<FilterConsumerBehavior>)serviceProvider.GetRequiredService<ILogger<FilterConsumerBehavior>>();
        _logger = serviceProvider.GetRequiredService<ISilverbackLogger<FilterConsumerBehavior>>();
    }

    [Fact]
    public async Task HandleAsync_ShouldCallNext_WhenNoFilterIsConfigured()
    {
        InboundEnvelope<TestEventOne> envelope = CreateEnvelope(null, new TestEventOne());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);
        bool nextCalled = false;

        await new FilterConsumerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        nextCalled.ShouldBeTrue();
        envelope.IsFiltered.ShouldBeFalse();
        _loggerSubstitute.DidNotReceive(LogLevel.Information, null, null, 1008);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallNext_WhenFilterAcceptsMessage()
    {
        InboundEnvelope<TestEventOne> envelope = CreateEnvelope(new TestFilter(), new TestEventOne { Content = "process" });
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);
        bool nextCalled = false;

        await new FilterConsumerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        nextCalled.ShouldBeTrue();
        envelope.IsFiltered.ShouldBeFalse();
        _loggerSubstitute.DidNotReceive(LogLevel.Information, null, null, 1008);
    }

    [Fact]
    public async Task HandleAsync_ShouldFlagAndLogMessageAndCallNext_WhenFilterRejectsMessage()
    {
        InboundEnvelope<TestEventOne> envelope = CreateEnvelope(
            new TestFilter(),
            new TestEventOne { Content = "discard" },
            new TestOffset("a", "42"));
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);
        bool? isFilteredInNext = null;

        await new FilterConsumerBehavior(_logger).HandleAsync(
            context,
            (nextContext, _) =>
            {
                isFilteredInNext = ((InboundEnvelope)nextContext.Envelope).IsFiltered;
                return default;
            },
            CancellationToken.None);

        isFilteredInNext.ShouldBe(true);
        envelope.IsFiltered.ShouldBeTrue();
        _loggerSubstitute.Received(LogLevel.Information, null, "Message filtered | EndpointName: test, BrokerMessageId: 42", 1008);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationTokenToFilter()
    {
        TestFilter filter = new();
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(CreateEnvelope(filter, new TestEventOne()));
        using CancellationTokenSource cancellationTokenSource = new();

        await new FilterConsumerBehavior(_logger).HandleAsync(context, (_, _) => default, cancellationTokenSource.Token);

        filter.CancellationTokens.ShouldBe([cancellationTokenSource.Token]);
    }

    [Fact]
    public async Task HandleAsync_ShouldRethrowAndNotCallNext_WhenFilterThrows()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            CreateEnvelope(new TestFilter(), new TestEventOne { Content = "throw" }));
        bool nextCalled = false;

        Task handleTask = new FilterConsumerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None).AsTask();

        await Should.ThrowAsync<InvalidOperationException>(handleTask);
        nextCalled.ShouldBeFalse();
    }

    [Theory]
    [InlineData("process", false)]
    [InlineData("discard", true)]
    public async Task HandleAsync_ShouldResolveFilterFromServiceProvider(string content, bool isFiltered)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services.AddSingleton<TestFilter>());
        InboundEnvelope<TestEventOne> envelope = CreateEnvelope(
            new ServiceProviderInboundMessageFilter(typeof(TestFilter)),
            new TestEventOne { Content = content });
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider);
        bool nextCalled = false;

        await new FilterConsumerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None);

        nextCalled.ShouldBeTrue();
        envelope.IsFiltered.ShouldBe(isFiltered);
        serviceProvider.GetRequiredService<TestFilter>().Envelopes.ShouldBe([envelope]);
    }

    [Fact]
    public async Task HandleAsync_ShouldResolveScopedFilterFromContextServiceProvider()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services.AddScoped<TestFilter>());
        using IServiceScope scope1 = serviceProvider.CreateScope();
        using IServiceScope scope2 = serviceProvider.CreateScope();
        InboundEnvelope<TestEventOne> envelope = CreateEnvelope(
            new ServiceProviderInboundMessageFilter(typeof(TestFilter)),
            new TestEventOne());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, scope1.ServiceProvider);

        await new FilterConsumerBehavior(_logger).HandleAsync(context, (_, _) => default, CancellationToken.None);

        scope1.ServiceProvider.GetRequiredService<TestFilter>().Envelopes.ShouldBe([envelope]);
        scope2.ServiceProvider.GetRequiredService<TestFilter>().Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenFilterTypeIsNotRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(_ => { });
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            CreateEnvelope(new ServiceProviderInboundMessageFilter(typeof(TestFilter)), new TestEventOne()),
            serviceProvider);
        bool nextCalled = false;

        Task handleTask = new FilterConsumerBehavior(_logger).HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return default;
            },
            CancellationToken.None).AsTask();

        await Should.ThrowAsync<InvalidOperationException>(handleTask);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenMessageIsNotDeserialized()
    {
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("test")
        {
            Filter = new TestFilter()
        }.GetDefaultEndpoint();
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            new RawInboundEnvelope(Stream.Null, [], endpoint, Substitute.For<IConsumer>(), new TestOffset()));

        Task handleTask = new FilterConsumerBehavior(_logger).HandleAsync(context, (_, _) => default, CancellationToken.None).AsTask();

        await Should.ThrowAsync<InvalidOperationException>(handleTask);
    }

    private static InboundEnvelope<TestEventOne> CreateEnvelope(
        IInboundMessageFilter? filter,
        TestEventOne message,
        TestOffset? offset = null)
    {
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("test")
        {
            Filter = filter
        }.GetDefaultEndpoint();

        return new InboundEnvelope<TestEventOne>(
            message,
            Stream.Null,
            [],
            endpoint,
            Substitute.For<IConsumer>(),
            offset ?? new TestOffset());
    }

    private sealed class TestFilter : IInboundMessageFilter
    {
        public List<IInboundEnvelope> Envelopes { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            CancellationTokens.Add(cancellationToken);

            return envelope.Message switch
            {
                TestEventOne { Content: "throw" } => throw new InvalidOperationException("Test"),
                TestEventOne { Content: "discard" } => ValueTask.FromResult(false),
                _ => ValueTask.FromResult(true)
            };
        }
    }
}
