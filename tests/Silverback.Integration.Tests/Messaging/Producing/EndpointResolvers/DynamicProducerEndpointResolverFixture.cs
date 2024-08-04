// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.EndpointResolvers;

public class DynamicProducerEndpointResolverFixture
{
    private readonly TestDynamicProducerEndpointResolver<TestEventOne> _endpointResolver = new("topic");

    [Fact]
    public void GetEndpoint_ShouldReturnEndpoint()
    {
        TestProducerEndpoint endpoint = _endpointResolver.GetEndpoint(
            new TestEventOne(),
            new TestProducerEndpointConfiguration(),
            Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.RawName.Should().Be("topic");
    }

    [Fact]
    public void GetEndpoint_ShouldReturnEndpointForTombstone()
    {
        TestProducerEndpoint endpoint = _endpointResolver.GetEndpoint(
            new Tombstone<TestEventOne>("123"),
            new TestProducerEndpointConfiguration(),
            Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.RawName.Should().Be("topic");
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenProducingCollection()
    {
        Action act = () => _endpointResolver.GetEndpoint(
            Array.Empty<TestEventOne>(),
            new TestProducerEndpointConfiguration(),
            Substitute.For<IServiceProvider>());

        act.Should().Throw<SilverbackConfigurationException>()
            .WithMessage(
                "Dynamic endpoint resolvers cannot be used to produce message batches. " +
                "Please use a static endpoint resolver instead.");
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenProducingEnumerable()
    {
        Action act = () => _endpointResolver.GetEndpoint(
            Enumerable.Empty<TestEventOne>(),
            new TestProducerEndpointConfiguration(),
            Substitute.For<IServiceProvider>());

        act.Should().Throw<SilverbackConfigurationException>()
            .WithMessage(
                "Dynamic endpoint resolvers cannot be used to produce message batches. " +
                "Please use a static endpoint resolver instead.");
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenProducingAsyncEnumerable()
    {
        Action act = () => _endpointResolver.GetEndpoint(
            AsyncEnumerable.Empty<TestEventOne>(),
            new TestProducerEndpointConfiguration(),
            Substitute.For<IServiceProvider>());

        act.Should().Throw<SilverbackConfigurationException>()
            .WithMessage(
                "Dynamic endpoint resolvers cannot be used to produce message batches. " +
                "Please use a static endpoint resolver instead.");
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenMessageTypeMismatch()
    {
        Action act = () => _endpointResolver.GetEndpoint(
            new TestEventTwo(),
            new TestProducerEndpointConfiguration(),
            Substitute.For<IServiceProvider>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The message type (TestEventTwo) doesn't match the expected type (TestEventOne).");
    }
}
