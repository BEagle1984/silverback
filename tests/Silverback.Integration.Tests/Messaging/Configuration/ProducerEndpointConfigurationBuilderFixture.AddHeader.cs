﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void AddHeader_ShouldAddMessageEnricher()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .AddHeader("key", "value")
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<StaticOutboundHeadersEnricher>();
    }

    [Fact]
    public void AddHeader_ShouldAddMessageEnricherForChildType()
    {
        TestProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .AddHeader<TestEventOne>("key", "value")
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<GenericOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void AddHeader_ShouldAddMessageEnricherWithValueFunction()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .AddHeader("key", message => message?.Content)
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<GenericOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void AddHeader_ShouldAddMessageEnricherWithValueFunctionForChildType()
    {
        TestProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .AddHeader<TestEventOne>("key", message => message?.Content)
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<GenericOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void AddHeader_ShouldAddMessageEnricherWithEnvelopeBasedValueFunction()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .AddHeader("key", envelope => envelope.Headers.Count)
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<GenericOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void AddHeader_ShouldAddMessageEnricherWithEnvelopeBasedValueFunctionForChildType()
    {
        TestProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .AddHeader<TestEventOne>("key", envelope => envelope.Headers.Count)
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<GenericOutboundHeadersEnricher<TestEventOne>>();
    }
}