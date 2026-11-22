// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Filter;
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
    ///     Gets the <see cref="IMessageDeserializer" /> to be used to deserialize the consumed messages.
    ///     The default is the <see cref="JsonMessageDeserializer{TMessage}" />.
    /// </summary>
    public IMessageDeserializer Deserializer { get; init; } = DefaultDeserializers.Json;

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
    /// <remarks>
    ///     When <c>false</c>, an unhandled message is ignored without being added to the sequence it belongs to, which can prevent a custom
    ///     sequence from being completed (see
    ///     <see cref="ConsumerEndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.IgnoreUnhandledMessages" />).
    /// </remarks>
    public bool ThrowIfUnhandled { get; init; } = true;

    /// <summary>
    ///     Gets the error policy to be applied when an exception occurs during the processing of the consumed messages.
    ///     The default is the <see cref="StopConsumerErrorPolicy" />.
    /// </summary>
    public IErrorPolicy ErrorPolicy { get; init; } = DefaultErrorPolicy;

    /// <summary>
    ///     Gets the encryption settings to be used to decrypt the encrypted messages. The default is <c>null</c>.
    /// </summary>
    public IDecryptionSettings? Encryption { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the subscribers can subscribe a stream (<see cref="IAsyncEnumerable{T}" />,
    ///     <see cref="IEnumerable{T}" />, etc.) pushed with all messages consumed from that endpoint (or partition). This setup can be used
    ///     for basic stream processing, but it's more fragile and should be used with care. Some features, like error policies, won't work
    ///     as expected. The default is <c>false</c>.
    /// </summary>
    public bool AllowStreaming { get; init; }

    /// <summary>
    ///     Gets the <see cref="IInboundMessageFilter" /> to be used to filter out the consumed messages that should not be processed. The
    ///     filtered messages aren't delivered to any subscriber and are committed (or acknowledged) like the messages that no subscriber
    ///     handles. The default is <c>null</c>.
    /// </summary>
    /// <remarks>
    ///     When the filter is set via
    ///     <see cref="ConsumerEndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.Filter{TFilter}()" />, the actual filter is
    ///     resolved from the service provider for each message by the consumer pipeline. In that case, the value of this property only
    ///     identifies the filter type and can't be invoked directly.
    ///     <para>
    ///         With batch processing, the filtered messages aren't part of any batch: Kafka commits them with the next commit of the same
    ///         partition, so a partition whose latest messages are all filtered out keeps them uncommitted until a message of the
    ///         partition is processed in a batch.
    ///     </para>
    ///     <para>
    ///         A filtered message isn't added to the sequence it belongs to and doesn't count toward its length. A custom
    ///         <see cref="ISequenceReader" /> whose sequences are completed by their last message, or by a total length equal to the fixed
    ///         number of messages they are made of, should therefore not be combined with a filter: a sequence whose last message (or any message, with a total length) is filtered out isn't
    ///         completed and is eventually aborted as incomplete. The built-in batches, chunked messages and streams aren't affected.
    ///     </para>
    /// </remarks>
    public IInboundMessageFilter? Filter { get; init; }

    /// <inheritdoc cref="EndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Deserializer == null)
            throw new BrokerConfigurationException($"{nameof(Deserializer)} is required.");

        if (Sequence == null)
            throw new BrokerConfigurationException($"{nameof(Sequence)} is required.");

        if (ErrorPolicy == null)
            throw new BrokerConfigurationException($"{nameof(ErrorPolicy)} is required.");

        Sequence.Validate();
        Batch?.Validate();
        Encryption?.Validate();

        if (AllowStreaming && Batch != null)
            throw new BrokerConfigurationException($"{nameof(Batch)} cannot be set when {nameof(AllowStreaming)} is true.");

        if (AllowStreaming && ErrorPolicy is not StopConsumerErrorPolicy)
            throw new BrokerConfigurationException($"{nameof(ErrorPolicy)} must be a {nameof(StopConsumerErrorPolicy)} when {nameof(AllowStreaming)} is true.");
    }
}
