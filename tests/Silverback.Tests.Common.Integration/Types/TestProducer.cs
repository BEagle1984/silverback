// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Types;

public class TestProducer : IProducer
{
    public TestProducer()
    {
        EnvelopeFactory = new TestOutboundEnvelopeFactory(this);
    }

    public string Name => "producer1-name";

    public string DisplayName => "producer1";

    public ProducerEndpointConfiguration EndpointConfiguration { get; } = TestProducerEndpointConfiguration.GetDefault();

    public IOutboundEnvelopeFactory EnvelopeFactory { get; }

    public IBrokerMessageIdentifier Produce<TMessage>(TMessage? message, Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class => throw new NotSupportedException();

    public IBrokerMessageIdentifier Produce(IOutboundEnvelope envelope) => throw new NotSupportedException();

    public void Produce<TMessage>(TMessage? message, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class => throw new NotSupportedException();

    public void Produce<TMessage, TState>(TMessage? message, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class => throw new NotSupportedException();

    public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

    public void Produce<TState>(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

    public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

    public IBrokerMessageIdentifier RawProduce(Stream? messageStream, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

    public IBrokerMessageIdentifier RawProduce(IOutboundEnvelope envelope) => throw new NotSupportedException();

    public void RawProduce(byte[]? messageContent, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

    public void RawProduce<TState>(byte[]? messageContent, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

    public void RawProduce(Stream? messageStream, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

    public void RawProduce<TState>(Stream? messageStream, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

    public void RawProduce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

    public void RawProduce<TState>(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

    public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(object? message, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? messageContent, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? messageStream, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
