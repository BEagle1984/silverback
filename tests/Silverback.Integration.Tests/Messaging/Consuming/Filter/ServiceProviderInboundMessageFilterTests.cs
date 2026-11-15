// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.Filter;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.Filter;

public class ServiceProviderInboundMessageFilterTests
{
    [Fact]
    public void Resolve_ShouldReturnFilterFromServiceProvider()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services.AddSingleton<TestFilter>());
        ServiceProviderInboundMessageFilter filter = new(typeof(TestFilter));

        IInboundMessageFilter resolvedFilter = filter.Resolve(serviceProvider);

        resolvedFilter.ShouldBeSameAs(serviceProvider.GetRequiredService<TestFilter>());
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenFilterTypeIsNotRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(_ => { });
        ServiceProviderInboundMessageFilter filter = new(typeof(TestFilter));

        Action act = () => filter.Resolve(serviceProvider);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public async Task ShouldProcessAsync_ShouldThrow()
    {
        InboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            Stream.Null,
            [],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());
        ServiceProviderInboundMessageFilter filter = new(typeof(TestFilter));

        Func<Task> act = async () => await filter.ShouldProcessAsync(envelope, CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenFilterTypeIsSame()
    {
        ServiceProviderInboundMessageFilter filter1 = new(typeof(TestFilter));
        ServiceProviderInboundMessageFilter filter2 = new(typeof(TestFilter));

        bool result = filter1.Equals(filter2);

        result.ShouldBeTrue();
        filter1.GetHashCode().ShouldBe(filter2.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenFilterTypeIsDifferent()
    {
        ServiceProviderInboundMessageFilter filter1 = new(typeof(TestFilter));
        ServiceProviderInboundMessageFilter filter2 = new(typeof(OtherTestFilter));

        bool result = filter1.Equals(filter2);

        result.ShouldBeFalse();
    }

    private sealed class TestFilter : IInboundMessageFilter
    {
        public ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken) =>
            ValueTask.FromResult(true);
    }

    private sealed class OtherTestFilter : IInboundMessageFilter
    {
        public ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken) =>
            ValueTask.FromResult(false);
    }
}
