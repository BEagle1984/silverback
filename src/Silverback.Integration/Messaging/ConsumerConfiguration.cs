// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging;

/// <summary>
///     The consumer configuration.
/// </summary>
public abstract record ConsumerConfiguration : EndpointConfiguration
{
    private static readonly IErrorPolicy DefaultErrorPolicy = new StopConsumerErrorPolicy();

    /// <summary>
    ///     Gets the batch settings. Can be used to enable and setup batch processing.
    ///     The default is <c>null</c>, which means that batch processing is disabled.
    /// </summary>
    public BatchSettings? Batch { get; init; }

    /// <summary>
    ///     Gets the sequence settings. A sequence is a set of related messages, like the chunks belonging to the same message or
    ///     the messages in a dataset.
    /// </summary>
    public SequenceSettings Sequence { get; init; } = new();

    /// <summary>
    ///     Gets a value indicating whether an exception must be thrown if no subscriber is handling the
    ///     received message. The default is <c>true</c>.
    /// </summary>
    public bool ThrowIfUnhandled { get; init; } = true;

    /// <summary>
    ///     Gets the error policy to be applied when an exception occurs during the processing of the consumed messages.
    ///     The default is the <see cref="StopConsumerErrorPolicy" />.
    /// </summary>
    public IErrorPolicy ErrorPolicy { get; init; } = DefaultErrorPolicy;

    /// <summary>
    ///     Gets the strategy to be used to guarantee that each message is consumed only once.
    /// </summary>
    public IExactlyOnceStrategy? ExactlyOnceStrategy { get; init; }

    /// <summary>
    ///     Gets a value indicating how to handle the null messages. The default is the
    ///     <see cref="Serialization.NullMessageHandlingStrategy.Tombstone" />.
    /// </summary>
    public NullMessageHandlingStrategy NullMessageHandlingStrategy { get; init; }

    /// <summary>
    ///     Gets a unique name for the consumer group (e.g. Kafka's consumer group id). This value (joint with
    ///     the endpoint name) will be used for example to ensure the exactly-once delivery.
    /// </summary>
    /// <remarks>
    ///     It's not enough to use the endpoint name, since the same topic could be consumed by multiple
    ///     consumer groups within the same process and/or using the same database to store the information
    ///     needed to ensure the exactly-once delivery.
    /// </remarks>
    /// <returns>
    ///     Returns the unique name for the consumer group.
    /// </returns>
    public abstract string GetUniqueConsumerGroupName();

    /// <inheritdoc cref="EndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Sequence == null)
            throw new EndpointConfigurationException("Sequence cannot be null.");

        if (ErrorPolicy == null)
            throw new EndpointConfigurationException("ErrorPolicy cannot be null.");

        Batch?.Validate();
    }
}
