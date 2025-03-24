// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics;

public class BrokerLogEnricherFactoryFixture
{
    [Fact]
    public void GetEnricher_ShouldReturnBrokerLogEnricherAccordingToEndpointConfigurationType()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new BrokerLogEnricher2());

        IBrokerLogEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher1.ShouldBeOfType<BrokerLogEnricher1>();
        enricher2.ShouldBeOfType<BrokerLogEnricher2>();
    }

    [Fact]
    public void GetEnricher_ShouldReturnNullEnricher_WhenFactoryNotRegistered()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());

        IBrokerLogEnricher enricher = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher.ShouldBe(NullBrokerLogEnricher.Instance);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedEnricherInstance()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new BrokerLogEnricher2());

        IBrokerLogEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());

        enricher2.ShouldBeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedEnricherInstance_WhenOverridden()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new BrokerLogEnricher2());

        factory.OverrideFactories(_ => new OverrideLogEnricher());

        EndpointConfiguration1 endpointConfiguration1 = new();
        IBrokerLogEnricher enricher1 = factory.GetEnricher(endpointConfiguration1, Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher2 = factory.GetEnricher(endpointConfiguration1, Substitute.For<IServiceProvider>());

        enricher2.ShouldBeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstanceByType()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new BrokerLogEnricher2());

        IBrokerLogEnricher enricher1A = factory.GetEnricher(new EndpointConfiguration1("A"), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher1B = factory.GetEnricher(new EndpointConfiguration1("B"), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher2A = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher2B = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher1A.ShouldBeSameAs(enricher1B);
        enricher2A.ShouldBeSameAs(enricher2B);
        enricher1A.ShouldNotBeSameAs(enricher2A);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstanceByType_WhenOverridden()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new BrokerLogEnricher2());
        factory.OverrideFactories(_ => new OverrideLogEnricher());

        IBrokerLogEnricher enricher1A = factory.GetEnricher(new EndpointConfiguration1("A"), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher1B = factory.GetEnricher(new EndpointConfiguration1("B"), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher2A = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher2B = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher1A.ShouldBeSameAs(enricher1B);
        enricher2A.ShouldBeSameAs(enricher2B);
        enricher1A.ShouldNotBeSameAs(enricher2A);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());

        Action act = () => factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The factory for the specified discriminator type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(_ => new BrokerLogEnricher2());

        factory.OverrideFactories(_ => new OverrideLogEnricher());

        IBrokerLogEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1(), Substitute.For<IServiceProvider>());
        IBrokerLogEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration2(), Substitute.For<IServiceProvider>());

        enricher1.ShouldBeOfType<OverrideLogEnricher>();
        enricher2.ShouldBeOfType<OverrideLogEnricher>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());

        bool result = factory.HasFactory<EndpointConfiguration1>();

        result.ShouldBeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(_ => new BrokerLogEnricher1());

        bool result = factory.HasFactory<EndpointConfiguration2>();

        result.ShouldBeFalse();
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record EndpointConfiguration1(string Property = "") : EndpointConfiguration;

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record EndpointConfiguration2(string Property = "") : EndpointConfiguration;

    private class BrokerLogEnricher1 : BrokerLogEnricher
    {
        protected override string AdditionalPropertyName1 => string.Empty;

        protected override string AdditionalPropertyName2 => string.Empty;

        public override (string? Value1, string? Value2) GetAdditionalValues(
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier) =>
            throw new NotSupportedException();
    }

    private class BrokerLogEnricher2 : BrokerLogEnricher
    {
        protected override string AdditionalPropertyName1 => string.Empty;

        protected override string AdditionalPropertyName2 => string.Empty;

        public override (string? Value1, string? Value2) GetAdditionalValues(
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier) =>
            throw new NotSupportedException();
    }

    private class OverrideLogEnricher : BrokerLogEnricher
    {
        protected override string AdditionalPropertyName1 => string.Empty;

        protected override string AdditionalPropertyName2 => string.Empty;

        public override (string? Value1, string? Value2) GetAdditionalValues(
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier) =>
            throw new NotSupportedException();
    }
}
