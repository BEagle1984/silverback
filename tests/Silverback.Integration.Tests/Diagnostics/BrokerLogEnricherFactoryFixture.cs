// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Diagnostics;
using Silverback.Messaging;
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
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(() => new BrokerLogEnricher2());

        IBrokerLogEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1());
        IBrokerLogEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration2());

        enricher1.Should().BeOfType<BrokerLogEnricher1>();
        enricher2.Should().BeOfType<BrokerLogEnricher2>();
    }

    [Fact]
    public void GetEnricher_ShouldReturnNullEnricher_WhenFactoryNotRegistered()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());

        IBrokerLogEnricher enricher = factory.GetEnricher(new EndpointConfiguration2());

        enricher.Should().Be(NullBrokerLogEnricher.Instance);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedEnricherInstance()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(() => new BrokerLogEnricher2());

        IBrokerLogEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1());
        IBrokerLogEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration1());

        enricher2.Should().BeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedEnricherInstance_WhenOverridden()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(() => new BrokerLogEnricher2());

        factory.OverrideFactories(() => new OverrideLogEnricher());

        EndpointConfiguration1 endpointConfiguration1 = new();
        IBrokerLogEnricher enricher1 = factory.GetEnricher(endpointConfiguration1);
        IBrokerLogEnricher enricher2 = factory.GetEnricher(endpointConfiguration1);

        enricher2.Should().BeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstanceByType()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(() => new BrokerLogEnricher2());

        IBrokerLogEnricher enricher1A = factory.GetEnricher(new EndpointConfiguration1("A"));
        IBrokerLogEnricher enricher1B = factory.GetEnricher(new EndpointConfiguration1("B"));
        IBrokerLogEnricher enricher2A = factory.GetEnricher(new EndpointConfiguration2());
        IBrokerLogEnricher enricher2B = factory.GetEnricher(new EndpointConfiguration2());

        enricher1A.Should().BeSameAs(enricher1B);
        enricher2A.Should().BeSameAs(enricher2B);
        enricher1A.Should().NotBeSameAs(enricher2A);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstanceByType_WhenOverridden()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(() => new BrokerLogEnricher2());
        factory.OverrideFactories(() => new OverrideLogEnricher());

        IBrokerLogEnricher enricher1A = factory.GetEnricher(new EndpointConfiguration1("A"));
        IBrokerLogEnricher enricher1B = factory.GetEnricher(new EndpointConfiguration1("B"));
        IBrokerLogEnricher enricher2A = factory.GetEnricher(new EndpointConfiguration2());
        IBrokerLogEnricher enricher2B = factory.GetEnricher(new EndpointConfiguration2());

        enricher1A.Should().BeSameAs(enricher1B);
        enricher2A.Should().BeSameAs(enricher2B);
        enricher1A.Should().NotBeSameAs(enricher2A);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());

        Action act = () => factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The factory for the specified discriminator type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());
        factory.AddFactory<EndpointConfiguration2>(() => new BrokerLogEnricher2());

        factory.OverrideFactories(() => new OverrideLogEnricher());

        IBrokerLogEnricher enricher1 = factory.GetEnricher(new EndpointConfiguration1());
        IBrokerLogEnricher enricher2 = factory.GetEnricher(new EndpointConfiguration2());

        enricher1.Should().BeOfType<OverrideLogEnricher>();
        enricher2.Should().BeOfType<OverrideLogEnricher>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());

        bool result = factory.HasFactory<EndpointConfiguration1>();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        BrokerLogEnricherFactory factory = new();
        factory.AddFactory<EndpointConfiguration1>(() => new BrokerLogEnricher1());

        bool result = factory.HasFactory<EndpointConfiguration2>();

        result.Should().BeFalse();
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
            Endpoint endpoint,
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier) =>
            throw new NotSupportedException();
    }

    private class BrokerLogEnricher2 : BrokerLogEnricher
    {
        protected override string AdditionalPropertyName1 => string.Empty;

        protected override string AdditionalPropertyName2 => string.Empty;

        public override (string? Value1, string? Value2) GetAdditionalValues(
            Endpoint endpoint,
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier) =>
            throw new NotSupportedException();
    }

    private class OverrideLogEnricher : BrokerLogEnricher
    {
        protected override string AdditionalPropertyName1 => string.Empty;

        protected override string AdditionalPropertyName2 => string.Empty;

        public override (string? Value1, string? Value2) GetAdditionalValues(
            Endpoint endpoint,
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier) =>
            throw new NotSupportedException();
    }
}
