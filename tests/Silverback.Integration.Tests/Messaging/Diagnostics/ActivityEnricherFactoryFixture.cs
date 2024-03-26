// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Diagnostics;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivityEnricherFactoryFixture
{
    [Fact]
    public void GetEnricher_ShouldReturnActivityEnricherAccordingToEndpointConfigurationType()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new ActivityEnricher2());

        IBrokerActivityEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher1.Should().BeOfType<ActivityEnricher1>();
        enricher2.Should().BeOfType<ActivityEnricher2>();
    }

    [Fact]
    public void GetEnricher_ShouldReturnNullEnricher_WhenFactoryNotRegistered()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());

        IBrokerActivityEnricher enricher = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher.Should().Be(NullBrokerActivityEnricher.Instance);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedEnricherInstance()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new ActivityEnricher2());

        IBrokerActivityEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());

        enricher2.Should().BeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedEnricherInstance_WhenOverridden()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new ActivityEnricher2());

        factory.OverrideFactories(_ => new OverrideActivityEnricher());

        EndpointConfiguration1 endpointConfiguration1 = new();
        IBrokerActivityEnricher enricher1 = factory.GetEnricher(endpointConfiguration1, Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher2 = factory.GetEnricher(endpointConfiguration1, Substitute.For<IServiceProvider>());

        enricher2.Should().BeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstanceByType()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new ActivityEnricher2());

        IBrokerActivityEnricher enricher1A = factory.GetEnricher(new EndpointConfiguration1("A"), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher1B = factory.GetEnricher(new EndpointConfiguration1("B"), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher2A = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher2B = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher1A.Should().BeSameAs(enricher1B);
        enricher2A.Should().BeSameAs(enricher2B);
        enricher1A.Should().NotBeSameAs(enricher2A);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstanceByType_WhenOverridden()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new ActivityEnricher2());
        factory.OverrideFactories(_ => new OverrideActivityEnricher());

        IBrokerActivityEnricher enricher1A = factory.GetEnricher(new EndpointConfiguration1("A"), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher1B = factory.GetEnricher(new EndpointConfiguration1("B"), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher2A = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher2B = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher1A.Should().BeSameAs(enricher1B);
        enricher2A.Should().BeSameAs(enricher2B);
        enricher1A.Should().NotBeSameAs(enricher2A);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());

        Action act = () => factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The factory for the specified discriminator type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new ActivityEnricher2());

        factory.OverrideFactories(_ => new OverrideActivityEnricher());

        IBrokerActivityEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerActivityEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher1.Should().BeOfType<OverrideActivityEnricher>();
        enricher2.Should().BeOfType<OverrideActivityEnricher>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());

        bool result = factory.HasFactory<EndpointConfiguration1>();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        ActivityEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new ActivityEnricher1());

        bool result = factory.HasFactory<EndpointConfiguration2>();

        result.Should().BeFalse();
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record EndpointConfiguration1(string Property = "") : EndpointConfiguration;

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record EndpointConfiguration2(string Property = "") : EndpointConfiguration;

    private class ActivityEnricher1 : IBrokerActivityEnricher
    {
        public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext producerContext) => throw new NotSupportedException();

        public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext consumerContext) => throw new NotSupportedException();
    }

    private class ActivityEnricher2 : IBrokerActivityEnricher
    {
        public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext producerContext) => throw new NotSupportedException();

        public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext consumerContext) => throw new NotSupportedException();
    }

    private class OverrideActivityEnricher : IBrokerActivityEnricher
    {
        public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext producerContext) => throw new NotSupportedException();

        public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext consumerContext) => throw new NotSupportedException();
    }
}
