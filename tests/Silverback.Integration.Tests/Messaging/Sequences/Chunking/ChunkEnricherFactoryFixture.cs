// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking;

public class ChunkEnricherFactoryFixture
{
    [Fact]
    public void GetEnricher_ShouldReturnChunkEnricherAccordingToEndpointType()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());
        factory.AddFactory<ProducerEndpointConfiguration2>(_ => new ChunkEnricher2());

        IChunkEnricher enricher1 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("topic1"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2 = factory.GetEnricher(
            new ProducerEndpointConfiguration2("topic1"),
            Substitute.For<IServiceProvider>());

        enricher1.Should().BeOfType<ChunkEnricher1>();
        enricher2.Should().BeOfType<ChunkEnricher2>();
    }

    [Fact]
    public void GetEnricher_ShouldThrow_WhenNullSettingsArePassed()
    {
        ChunkEnricherFactory factory = new();

        Action act = () => factory.GetEnricher(null!, Substitute.For<IServiceProvider>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetEnricher_ShouldReturnNullEnricher_WhenFactoryNotRegistered()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());

        IChunkEnricher enricher1 = factory.GetEnricher(
            new ProducerEndpointConfiguration2("topic1"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2 = factory.GetEnricher(
            new ProducerEndpointConfiguration2("topic2"),
            Substitute.For<IServiceProvider>());

        enricher1.Should().BeOfType<NullChunkEnricher>();
        enricher2.Should().BeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstance()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());
        factory.AddFactory<ProducerEndpointConfiguration2>(_ => new ChunkEnricher2());

        IChunkEnricher enricher1 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("topic1"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("topic1"),
            Substitute.For<IServiceProvider>());

        enricher2.Should().BeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstance_WhenOverridden()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());
        factory.AddFactory<ProducerEndpointConfiguration2>(_ => new ChunkEnricher2());

        factory.OverrideFactories(_ => new OverrideChunkEnricher());

        ProducerEndpointConfiguration endpoint1 = new ProducerEndpointConfiguration1("topic1");
        IChunkEnricher enricher1 = factory.GetEnricher(endpoint1, Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2 = factory.GetEnricher(endpoint1, Substitute.For<IServiceProvider>());

        enricher1.Should().BeOfType<OverrideChunkEnricher>();
        enricher2.Should().BeSameAs(enricher1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstanceByTypeRegardlessOfSettings()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());
        factory.AddFactory<ProducerEndpointConfiguration2>(_ => new ChunkEnricher2());

        IChunkEnricher enricher1A1 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("A"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher1A2 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("A"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher1B1 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("B"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher1B2 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("B"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2A1 = factory.GetEnricher(
            new ProducerEndpointConfiguration2("A"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2A2 = factory.GetEnricher(
            new ProducerEndpointConfiguration2("A"),
            Substitute.For<IServiceProvider>());

        enricher1A1.Should().BeSameAs(enricher1A2);
        enricher1B1.Should().BeSameAs(enricher1B2);
        enricher1A1.Should().BeSameAs(enricher1B1);
        enricher2A1.Should().BeSameAs(enricher2A2);
        enricher2A1.Should().NotBeSameAs(enricher1A1);
    }

    [Fact]
    public void GetEnricher_ShouldReturnCachedInstanceRegardlessOfTypeAndSettings_WhenOverridden()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());
        factory.AddFactory<ProducerEndpointConfiguration2>(_ => new ChunkEnricher2());
        factory.OverrideFactories(_ => new OverrideChunkEnricher());

        IChunkEnricher enricher1A1 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("A"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher1A2 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("A"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher1B1 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("B"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher1B2 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("B"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2A1 = factory.GetEnricher(
            new ProducerEndpointConfiguration2("A"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2A2 = factory.GetEnricher(
            new ProducerEndpointConfiguration2("A"),
            Substitute.For<IServiceProvider>());

        enricher1A1.Should().BeSameAs(enricher1A2);
        enricher1B1.Should().BeSameAs(enricher1B2);
        enricher1A1.Should().BeSameAs(enricher1B1);
        enricher2A1.Should().BeSameAs(enricher2A2);
        enricher2A1.Should().NotBeSameAs(enricher1A1);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());

        Action act = () => factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The factory for the specified discriminator type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());
        factory.AddFactory<ProducerEndpointConfiguration2>(_ => new ChunkEnricher2());

        factory.OverrideFactories(_ => new OverrideChunkEnricher());

        IChunkEnricher enricher1 = factory.GetEnricher(
            new ProducerEndpointConfiguration1("topic1"),
            Substitute.For<IServiceProvider>());
        IChunkEnricher enricher2 = factory.GetEnricher(
            new ProducerEndpointConfiguration2("topic2"),
            Substitute.For<IServiceProvider>());

        enricher1.Should().BeOfType<OverrideChunkEnricher>();
        enricher2.Should().BeOfType<OverrideChunkEnricher>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());

        bool result = factory.HasFactory<ProducerEndpointConfiguration1>();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        ChunkEnricherFactory factory = new();
        factory.AddFactory<ProducerEndpointConfiguration1>(_ => new ChunkEnricher1());

        bool result = factory.HasFactory<ProducerEndpointConfiguration2>();

        result.Should().BeFalse();
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test class")]
    private record ProducerEndpointConfiguration1(string Topic) : ProducerEndpointConfiguration;

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test class")]
    private record ProducerEndpointConfiguration2(string Topic) : ProducerEndpointConfiguration;

    private class ChunkEnricher1 : IChunkEnricher
    {
        public MessageHeader GetFirstChunkMessageHeader(IOutboundEnvelope envelope) => throw new NotSupportedException();
    }

    private class ChunkEnricher2 : IChunkEnricher
    {
        public MessageHeader GetFirstChunkMessageHeader(IOutboundEnvelope envelope) => throw new NotSupportedException();
    }

    private class OverrideChunkEnricher : IChunkEnricher
    {
        public MessageHeader GetFirstChunkMessageHeader(IOutboundEnvelope envelope) => throw new NotSupportedException();
    }
}
