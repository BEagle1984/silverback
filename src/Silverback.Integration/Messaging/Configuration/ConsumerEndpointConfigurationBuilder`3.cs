// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="ConsumerEndpointConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being consumed.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the configuration being built.
/// </typeparam>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
[SuppressMessage("Design", "CA1005:Avoid excessive parameters on generic types", Justification = "Not instantiated directly")]
public abstract partial class ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    : EndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    where TConfiguration : ConsumerEndpointConfiguration
    where TBuilder : ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
{
    private IMessageDeserializer? _deserializer;

    private IErrorPolicy? _errorPolicy;

    private int? _batchSize;

    private TimeSpan? _batchMaxWaitTime;

    private TimeSpan? _sequenceTimeout;

    private bool? _throwIfUnhandled;

    private IDecryptionSettings? _encryptionSettings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerEndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///   The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="friendlyName">
    ///     An optional friendly to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </param>
    protected ConsumerEndpointConfigurationBuilder(IServiceProvider serviceProvider, string? friendlyName)
        : base(serviceProvider, friendlyName)
    {
        // Initialize default serializer according to TMessage type parameter
        if (typeof(IBinaryMessage).IsAssignableFrom(typeof(TMessage)))
            ConsumeBinaryMessages();
        else if (typeof(StringMessage).IsAssignableFrom(typeof(TMessage)))
            ConsumeStrings();
        else if (typeof(RawMessage).IsAssignableFrom(typeof(TMessage)))
            ConsumeRaw();
        else
            DeserializeJson();
    }

    /// <summary>
    ///     Specifies the error policy to be applied when an exception occurs during the processing of the consumed messages.
    /// </summary>
    /// <param name="errorPolicy">
    ///     The <see cref="IErrorPolicy" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder OnError(IErrorPolicy errorPolicy)
    {
        _errorPolicy = Check.NotNull(errorPolicy, nameof(errorPolicy));
        return This;
    }

    /// <summary>
    ///     Specifies the error policy to be applied when an exception occurs during the processing of the consumed messages.
    /// </summary>
    /// <param name="errorPolicyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="ErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder OnError(Action<IErrorPolicyBuilder> errorPolicyBuilderAction)
    {
        Check.NotNull(errorPolicyBuilderAction, nameof(errorPolicyBuilderAction));

        ErrorPolicyBuilder errorPolicyBuilder = new();
        errorPolicyBuilderAction.Invoke(errorPolicyBuilder);

        return OnError(errorPolicyBuilder.Build());
    }

    /// <summary>
    ///     Enables batch processing.
    /// </summary>
    /// <param name="batchSize">
    ///     The number of messages to be processed in batch.
    /// </param>
    /// <param name="maxWaitTime">
    ///     The maximum amount of time to wait for the batch to be filled. After this time the batch will be
    ///     completed even if the specified <c>batchSize</c> is not reached.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder EnableBatchProcessing(int batchSize, TimeSpan? maxWaitTime = null)
    {
        if (batchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "batchSize must be greater or equal to 1.");

        _batchSize = batchSize;
        _batchMaxWaitTime = maxWaitTime;

        return This;
    }

    /// <summary>
    ///     Sets the timeout after which an incomplete sequence that isn't pushed with new messages will be aborted and discarded.
    ///     The default is a conservative 30 minutes.
    /// </summary>
    /// <remarks>
    ///     This setting is ignored for batches (<see cref="BatchSequence" />), use the <c>maxWaitTime</c> parameter of the
    ///     <see cref="EnableBatchProcessing" /> method instead.
    /// </remarks>
    /// <param name="timeout">
    ///     The timeout.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithSequenceTimeout(TimeSpan timeout)
    {
        _sequenceTimeout = timeout;
        return This;
    }

    /// <summary>
    ///     Specifies that an exception must be thrown if no subscriber is handling the received message. This option is enabled by
    ///     default. Use the <see cref="IgnoreUnhandledMessages" /> method to disable it.
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ThrowIfUnhandled()
    {
        _throwIfUnhandled = true;
        return This;
    }

    /// <summary>
    ///     Specifies that the message has to be silently ignored if no subscriber is handling it.
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder IgnoreUnhandledMessages()
    {
        _throwIfUnhandled = false;
        return This;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.Build" />
    public sealed override TConfiguration Build()
    {
        TConfiguration configuration = base.Build();

        configuration = configuration with
        {
            Deserializer = _deserializer ?? configuration.Deserializer,
            ErrorPolicy = _errorPolicy ?? configuration.ErrorPolicy,
            Batch = _batchSize == null
                ? configuration.Batch
                : new BatchSettings
                {
                    Size = _batchSize.Value,
                    MaxWaitTime = _batchMaxWaitTime
                },
            Sequence = configuration.Sequence with
            {
                Timeout = _sequenceTimeout ?? configuration.Sequence.Timeout
            },
            ThrowIfUnhandled = _throwIfUnhandled ?? configuration.ThrowIfUnhandled,
            Encryption = _encryptionSettings ?? configuration.Encryption
        };

        configuration.Validate();

        return configuration;
    }
}
