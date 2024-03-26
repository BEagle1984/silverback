// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.BrokerMessageIdentifiersTracking;

public class BrokerMessageIdentifiersTrackerFactoryFixture
{
    [Fact]
    public void GetTracker_ShouldReturnTrackerAccordingToEndpointConfigurationType()
    {
        BrokerMessageIdentifiersTrackerFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());
        factory.AddFactory<EndpointConfiguration2>(_ => new Tracker2());

        IBrokerMessageIdentifiersTracker tracker1 = factory.GetTracker(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerMessageIdentifiersTracker tracker2 = factory.GetTracker(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        tracker1.Should().BeOfType<Tracker1>();
        tracker2.Should().BeOfType<Tracker2>();
    }

    [Fact]
    public void GetTracker_ShouldReturnSimpleTracker_WhenFactoryNotRegistered()
    {
        BrokerMessageIdentifiersTrackerFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());

        IBrokerMessageIdentifiersTracker tracker = factory.GetTracker(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        tracker.Should().BeOfType<SimpleMessageIdentifiersTracker>();
    }

    [Fact]
    public void GetTracker_ShouldReturnNewTrackerInstance()
    {
        BrokerMessageIdentifiersTrackerFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());
        factory.AddFactory<EndpointConfiguration2>(_ => new Tracker2());

        IBrokerMessageIdentifiersTracker tracker1 = factory.GetTracker(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerMessageIdentifiersTracker tracker2 = factory.GetTracker(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());

        tracker2.Should().NotBeSameAs(tracker1);
    }

    [Fact]
    public void GetTracker_ShouldReturnNewTrackerInstance_WhenOverridden()
    {
        BrokerMessageIdentifiersTrackerFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());
        factory.AddFactory<EndpointConfiguration2>(_ => new Tracker2());

        factory.OverrideFactories(_ => new OverrideTracker());

        EndpointConfiguration1 endpointConfiguration1 = new();
        IBrokerMessageIdentifiersTracker tracker1 = factory.GetTracker(endpointConfiguration1, Substitute.For<IServiceProvider>());
        IBrokerMessageIdentifiersTracker tracker2 = factory.GetTracker(endpointConfiguration1, Substitute.For<IServiceProvider>());

        tracker2.Should().NotBeSameAs(tracker1);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        BrokerMessageIdentifiersTrackerFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());

        Action act = () => factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The factory for the specified discriminator type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        BrokerMessageIdentifiersTrackerFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());
        factory.AddFactory<EndpointConfiguration2>(_ => new Tracker2());

        factory.OverrideFactories(_ => new OverrideTracker());

        IBrokerMessageIdentifiersTracker tracker1 = factory.GetTracker(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerMessageIdentifiersTracker tracker2 = factory.GetTracker(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        tracker1.Should().BeOfType<OverrideTracker>();
        tracker2.Should().BeOfType<OverrideTracker>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        BrokerMessageIdentifiersTrackerFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());

        bool result = factory.HasFactory<EndpointConfiguration1>();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        BrokerMessageIdentifiersTrackerFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new Tracker1());

        bool result = factory.HasFactory<EndpointConfiguration2>();

        result.Should().BeFalse();
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record EndpointConfiguration1(string Property = "") : EndpointConfiguration;

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record EndpointConfiguration2(string Property = "") : EndpointConfiguration;

    private class Tracker1 : IBrokerMessageIdentifiersTracker
    {
        public void TrackIdentifier(IBrokerMessageIdentifier identifier) => throw new NotSupportedException();

        public IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers() => throw new NotSupportedException();

        public IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers() => throw new NotSupportedException();
    }

    private class Tracker2 : IBrokerMessageIdentifiersTracker
    {
        public void TrackIdentifier(IBrokerMessageIdentifier identifier) => throw new NotSupportedException();

        public IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers() => throw new NotSupportedException();

        public IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers() => throw new NotSupportedException();
    }

    private class OverrideTracker : IBrokerMessageIdentifiersTracker
    {
        public void TrackIdentifier(IBrokerMessageIdentifier identifier) => throw new NotSupportedException();

        public IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers() => throw new NotSupportedException();

        public IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers() => throw new NotSupportedException();
    }
}
