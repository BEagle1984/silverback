// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Filter;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderTests
{
    [Fact]
    public void Filter_ShouldSetFilterFromMessagePredicate()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Filter(message => message?.Content == "produce").Build();

        configuration.Filter.ShouldBeOfType<GenericOutboundMessageFilter<TestEventOne>>();
    }

    [Fact]
    public void Filter_ShouldSetFilterFromEnvelopePredicate()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Filter(envelope => envelope.Message?.Content == "produce").Build();

        configuration.Filter.ShouldBeOfType<GenericOutboundMessageFilter<TestEventOne>>();
    }

    [Fact]
    public void Filter_ShouldSetFilterInstance()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());
        TestOutboundMessageFilter filter = new();

        TestProducerEndpointConfiguration configuration = builder.Filter(filter).Build();

        configuration.Filter.ShouldBeSameAs(filter);
    }

    [Fact]
    public void Filter_ShouldSetFilterType()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Filter<TestOutboundMessageFilter>().Build();

        ServiceProviderOutboundMessageFilter filter = configuration.Filter.ShouldBeOfType<ServiceProviderOutboundMessageFilter>();
        filter.FilterType.ShouldBe(typeof(TestOutboundMessageFilter));
    }

    [Fact]
    public void Filter_ShouldReplacePreviousFilter_WhenFilterInstanceIsSetLast()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());
        TestOutboundMessageFilter filter = new();

        TestProducerEndpointConfiguration configuration = builder
            .Filter(message => message?.Content == "produce")
            .Filter<TestOutboundMessageFilter>()
            .Filter(filter)
            .Build();

        configuration.Filter.ShouldBeSameAs(filter);
    }

    [Fact]
    public void Filter_ShouldReplacePreviousFilter_WhenFilterTypeIsSetLast()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder
            .Filter(new TestOutboundMessageFilter())
            .Filter(message => message?.Content == "produce")
            .Filter<OtherTestOutboundMessageFilter>()
            .Build();

        ServiceProviderOutboundMessageFilter filter = configuration.Filter.ShouldBeOfType<ServiceProviderOutboundMessageFilter>();
        filter.FilterType.ShouldBe(typeof(OtherTestOutboundMessageFilter));
    }

    [Fact]
    public void Build_ShouldReturnEqualConfigurations_WhenFilterTypeIsSame()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder1 = new(Substitute.For<IServiceProvider>());
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder2 = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration1 = builder1.Filter<TestOutboundMessageFilter>().Build();
        TestProducerEndpointConfiguration configuration2 = builder2.Filter<TestOutboundMessageFilter>().Build();

        configuration1.ShouldBe(configuration2);
    }

    [Fact]
    public void Build_ShouldReturnDifferentConfigurations_WhenFilterTypeIsDifferent()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder1 = new(Substitute.For<IServiceProvider>());
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder2 = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration1 = builder1.Filter<TestOutboundMessageFilter>().Build();
        TestProducerEndpointConfiguration configuration2 = builder2.Filter<OtherTestOutboundMessageFilter>().Build();

        configuration1.ShouldNotBe(configuration2);
    }

    private sealed class TestOutboundMessageFilter : IOutboundMessageFilter
    {
        public ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken) =>
            ValueTask.FromResult(true);
    }

    private sealed class OtherTestOutboundMessageFilter : IOutboundMessageFilter
    {
        public ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken) =>
            ValueTask.FromResult(false);
    }
}
