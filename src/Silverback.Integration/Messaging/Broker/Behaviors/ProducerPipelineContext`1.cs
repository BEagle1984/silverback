// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Behaviors;

/// <inheritdoc cref="ProducerPipelineContext" />
public class ProducerPipelineContext<TState> : ProducerPipelineContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProducerPipelineContext{TState}" /> class.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="producer">
    ///     The <see cref="IProducer" /> that triggered this pipeline.
    /// </param>
    /// <param name="pipeline">
    ///     The behaviors composing the pipeline.
    /// </param>
    /// <param name="finalAction">
    ///     The final action to be executed after the pipeline has been processed.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    public ProducerPipelineContext(
        IOutboundEnvelope envelope,
        IProducer producer,
        IReadOnlyList<IProducerBehavior> pipeline,
        ProducerBehaviorHandler finalAction,
        IServiceProvider serviceProvider)
        : base(envelope, producer, pipeline, finalAction, serviceProvider)
    {
    }

    internal Action<IBrokerMessageIdentifier?, TState>? OnSuccess { get; set; }

    internal Action<Exception, TState>? OnError { get; set; }

    internal TState? CallbackState { get; set; }

    /// <inheritdoc cref="ProducerPipelineContext.Clone" />
    public override ProducerPipelineContext Clone(IOutboundEnvelope? newEnvelope = null) =>
        new ProducerPipelineContext<TState>(newEnvelope ?? Envelope, Producer, Pipeline, FinalAction, ServiceProvider)
        {
            CurrentStepIndex = CurrentStepIndex,
            BrokerMessageIdentifier = BrokerMessageIdentifier,
            OnSuccess = OnSuccess,
            OnError = OnError,
            CallbackState = CallbackState
        };
}
