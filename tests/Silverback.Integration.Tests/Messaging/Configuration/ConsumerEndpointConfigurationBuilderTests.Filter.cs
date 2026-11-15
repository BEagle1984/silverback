// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Consuming.Filter;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ConsumerEndpointConfigurationBuilderTests
{
    [Fact]
    public void Filter_ShouldBeNullByDefault()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.Build();

        configuration.Filter.ShouldBeNull();
    }

    [Fact]
    public void Filter_ShouldSetFilterFromMessagePredicate()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.Filter(message => message?.Content == "process").Build();

        configuration.Filter.ShouldBeOfType<GenericInboundMessageFilter<TestEventOne>>();
    }

    [Fact]
    public void Filter_ShouldSetFilterFromEnvelopePredicate()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.Filter(envelope => envelope.Message?.Content == "process").Build();

        configuration.Filter.ShouldBeOfType<GenericInboundMessageFilter<TestEventOne>>();
    }

    [Fact]
    public void Filter_ShouldSetFilterInstance()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());
        TestInboundMessageFilter filter = new();

        TestConsumerEndpointConfiguration configuration = builder.Filter(filter).Build();

        configuration.Filter.ShouldBeSameAs(filter);
    }

    [Fact]
    public void Filter_ShouldSetFilterType()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.Filter<TestInboundMessageFilter>().Build();

        ServiceProviderInboundMessageFilter filter = configuration.Filter.ShouldBeOfType<ServiceProviderInboundMessageFilter>();
        filter.FilterType.ShouldBe(typeof(TestInboundMessageFilter));
    }

    [Fact]
    public void Filter_ShouldReplacePreviousFilter_WhenFilterInstanceIsSetLast()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());
        TestInboundMessageFilter filter = new();

        TestConsumerEndpointConfiguration configuration = builder
            .Filter(message => message?.Content == "process")
            .Filter<TestInboundMessageFilter>()
            .Filter(filter)
            .Build();

        configuration.Filter.ShouldBeSameAs(filter);
    }

    [Fact]
    public void Filter_ShouldReplacePreviousFilter_WhenFilterTypeIsSetLast()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder
            .Filter(new TestInboundMessageFilter())
            .Filter(message => message?.Content == "process")
            .Filter<OtherTestInboundMessageFilter>()
            .Build();

        ServiceProviderInboundMessageFilter filter = configuration.Filter.ShouldBeOfType<ServiceProviderInboundMessageFilter>();
        filter.FilterType.ShouldBe(typeof(OtherTestInboundMessageFilter));
    }

    [Fact]
    public void Filter_ShouldReplacePreviousFilter_WhenPredicateIsSetLast()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder
            .Filter<TestInboundMessageFilter>()
            .Filter(new TestInboundMessageFilter())
            .Filter(envelope => envelope.Message?.Content == "process")
            .Build();

        configuration.Filter.ShouldBeOfType<GenericInboundMessageFilter<TestEventOne>>();
    }

    [Fact]
    public void Build_ShouldReturnEqualConfigurations_WhenFilterTypeIsSame()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder1 = new(Substitute.For<IServiceProvider>());
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder2 = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration1 = builder1.Filter<TestInboundMessageFilter>().Build();
        TestConsumerEndpointConfiguration configuration2 = builder2.Filter<TestInboundMessageFilter>().Build();

        configuration1.ShouldBe(configuration2);
    }

    [Fact]
    public void Build_ShouldReturnDifferentConfigurations_WhenFilterTypeIsDifferent()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder1 = new(Substitute.For<IServiceProvider>());
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder2 = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration1 = builder1.Filter<TestInboundMessageFilter>().Build();
        TestConsumerEndpointConfiguration configuration2 = builder2.Filter<OtherTestInboundMessageFilter>().Build();

        configuration1.ShouldNotBe(configuration2);
    }

    private sealed class TestInboundMessageFilter : IInboundMessageFilter
    {
        public ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken) =>
            ValueTask.FromResult(true);
    }

    private sealed class OtherTestInboundMessageFilter : IInboundMessageFilter
    {
        public ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken) =>
            ValueTask.FromResult(false);
    }
}
