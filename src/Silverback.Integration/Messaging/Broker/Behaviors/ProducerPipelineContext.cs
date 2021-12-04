// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

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
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    public ProducerPipelineContext(IOutboundEnvelope envelope, IProducer producer, IServiceProvider serviceProvider)
    {
        Envelope = envelope;
        Producer = producer;
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    ///     Gets the <see cref="IProducer" /> that triggered this pipeline.
    /// </summary>
    public IProducer Producer { get; }

    /// <summary>
    ///     Gets or sets the envelope containing the message to be produced.
    /// </summary>
    public IOutboundEnvelope Envelope { get; set; }

    /// <summary>
    ///     Gets or sets the <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; }
}
