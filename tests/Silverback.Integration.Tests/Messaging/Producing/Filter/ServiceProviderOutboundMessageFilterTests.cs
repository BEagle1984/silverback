// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Filter;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Filter;

public class ServiceProviderOutboundMessageFilterTests
{
    [Fact]
    public void Resolve_ShouldReturnFilterFromServiceProvider()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services.AddSingleton<TestFilter>());
        ServiceProviderOutboundMessageFilter filter = new(typeof(TestFilter));

        IOutboundMessageFilter resolvedFilter = filter.Resolve(serviceProvider);

        resolvedFilter.ShouldBeSameAs(serviceProvider.GetRequiredService<TestFilter>());
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenFilterTypeIsNotRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(_ => { });
        ServiceProviderOutboundMessageFilter filter = new(typeof(TestFilter));

        Action act = () => filter.Resolve(serviceProvider);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public async Task ShouldProduceAsync_ShouldThrow()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());
        ServiceProviderOutboundMessageFilter filter = new(typeof(TestFilter));

        Func<Task> act = async () => await filter.ShouldProduceAsync(envelope, CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenFilterTypeIsSame()
    {
        ServiceProviderOutboundMessageFilter filter1 = new(typeof(TestFilter));
        ServiceProviderOutboundMessageFilter filter2 = new(typeof(TestFilter));

        bool result = filter1.Equals(filter2);

        result.ShouldBeTrue();
        filter1.GetHashCode().ShouldBe(filter2.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenFilterTypeIsDifferent()
    {
        ServiceProviderOutboundMessageFilter filter1 = new(typeof(TestFilter));
        ServiceProviderOutboundMessageFilter filter2 = new(typeof(OtherTestFilter));

        bool result = filter1.Equals(filter2);

        result.ShouldBeFalse();
    }

    private sealed class TestFilter : IOutboundMessageFilter
    {
        public ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken) =>
            ValueTask.FromResult(true);
    }

    private sealed class OtherTestFilter : IOutboundMessageFilter
    {
        public ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken) =>
            ValueTask.FromResult(false);
    }
}
