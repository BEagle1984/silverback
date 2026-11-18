// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors;

/// <summary>
///     The context that is passed along the producer behaviors pipeline.
/// </summary>
public class ProducerPipelineContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProducerPipelineContext" /> class.
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
    {
        Envelope = Check.NotNull(envelope, nameof(envelope));
        Producer = Check.NotNull(producer, nameof(producer));
        Pipeline = Check.NotNull(pipeline, nameof(pipeline));
        FinalAction = Check.NotNull(finalAction, nameof(finalAction));
        ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    /// <summary>
    ///     Gets the <see cref="IProducer" /> that triggered this pipeline.
    /// </summary>
    public IProducer Producer { get; }

    /// <summary>
    ///     Gets the behaviors composing the pipeline.
    /// </summary>
    public IReadOnlyList<IProducerBehavior> Pipeline { get; }

    /// <summary>
    ///     Gets the final action to be executed after the pipeline has been processed.
    /// </summary>
    public ProducerBehaviorHandler FinalAction { get; }

    /// <summary>
    ///     Gets or sets the envelope containing the message to be produced.
    /// </summary>
    public IOutboundEnvelope Envelope { get; set; }

    /// <summary>
    ///     Gets or sets the <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    ///     Gets the index of the current step in the pipeline.
    /// </summary>
    public int CurrentStepIndex { get; internal set; }

    internal IBrokerMessageIdentifier? BrokerMessageIdentifier { get; set; }

    // The produce activity started by the activity behavior for this pass of the pipeline. It's enriched and set as current when logging
    // the produced message, since it isn't necessarily the current activity anymore (e.g. a behavior started another one, or the message
    // is acknowledged from another thread).
    internal Activity? Activity { get; set; }

    /// <summary>
    ///     Clones the current context, optionally replacing the envelope.
    /// </summary>
    /// <param name="newEnvelope">
    ///     The new envelope to be used in the cloned context.
    /// </param>
    /// <returns>
    ///     The cloned context.
    /// </returns>
    public virtual ProducerPipelineContext Clone(IOutboundEnvelope? newEnvelope = null) =>
        new(newEnvelope ?? Envelope, Producer, Pipeline, FinalAction, ServiceProvider)
        {
            CurrentStepIndex = CurrentStepIndex,
            BrokerMessageIdentifier = BrokerMessageIdentifier,
            Activity = Activity
        };

    // Hands the produce activity over to be stopped when the produced messages are acknowledged, if they are acknowledged via callbacks
    internal virtual bool TryDeferActivityStop(Activity activity) => false;
}
