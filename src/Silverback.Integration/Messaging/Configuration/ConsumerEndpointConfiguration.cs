// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     The consumer configuration.
/// </summary>
public abstract record ConsumerEndpointConfiguration : EndpointConfiguration
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
    ///     Gets a value indicating how to handle the null messages. The default is the
    ///     <see cref="Serialization.NullMessageHandlingStrategy.Tombstone" />.
    /// </summary>
    public NullMessageHandlingStrategy NullMessageHandlingStrategy { get; init; }

    /// <summary>
    ///     Gets the encryption settings to be used to decrypt the encrypted messages. The default is <c>null</c>.
    /// </summary>
    public IDecryptionSettings? Encryption { get; init; }

    /// <inheritdoc cref="EndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Sequence == null)
            throw new BrokerConfigurationException("The sequence configuration is required.");

        if (ErrorPolicy == null)
            throw new BrokerConfigurationException("An error policy is required.");

        Batch?.Validate();
        Encryption?.Validate();
    }
}
