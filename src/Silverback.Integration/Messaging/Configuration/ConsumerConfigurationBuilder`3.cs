﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="ConsumerConfiguration" />.
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
public abstract partial class ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    : EndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    where TConfiguration : ConsumerConfiguration
    where TBuilder : ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder>
{
    private IErrorPolicy? _errorPolicy;

    private IExactlyOnceStrategy? _exactlyOnceStrategy;

    private int? _batchSize;

    private TimeSpan? _batchMaxWaitTime;

    private TimeSpan? _sequenceTimeout;

    private bool? _throwIfUnhandled;

    private NullMessageHandlingStrategy? _nullMessageHandling;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerConfigurationBuilder{TMessage,TConfiguration,TBuilder}" /> class.
    /// </summary>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    protected ConsumerConfigurationBuilder(EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        : base(endpointsConfigurationBuilder)
    {
        // Initialize default serializer according to TMessage type parameter
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
    public TBuilder OnError(Action<ErrorPolicyBuilder> errorPolicyBuilderAction)
    {
        Check.NotNull(errorPolicyBuilderAction, nameof(errorPolicyBuilderAction));

        ErrorPolicyBuilder errorPolicyBuilder = new(EndpointsConfigurationBuilder);
        errorPolicyBuilderAction.Invoke(errorPolicyBuilder);

        return OnError(errorPolicyBuilder.Build());
    }

    /// <summary>
    ///     Specifies the strategy to be used to ensure that each message is processed exactly once.
    /// </summary>
    /// <param name="strategy">
    ///     The <see cref="IExactlyOnceStrategy" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder EnsureExactlyOnce(IExactlyOnceStrategy strategy)
    {
        _exactlyOnceStrategy = Check.NotNull(strategy, nameof(strategy));
        return This;
    }

    /// <summary>
    ///     Specifies the strategy to be used to ensure that each message is processed exactly once.
    /// </summary>
    /// <param name="strategyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="ExactlyOnceStrategyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder EnsureExactlyOnce(Action<ExactlyOnceStrategyBuilder> strategyBuilderAction)
    {
        Check.NotNull(strategyBuilderAction, nameof(strategyBuilderAction));

        ExactlyOnceStrategyBuilder strategyBuilder = new();
        strategyBuilderAction.Invoke(strategyBuilder);

        return EnsureExactlyOnce(strategyBuilder.Build());
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
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                batchSize,
                "batchSize must be greater or equal to 1.");
        }

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

    /// <summary>
    ///     Specifies that the null messages have to be mapped to a <see cref="Tombstone{TMessage}" />
    ///     (<see cref="NullMessageHandlingStrategy.Tombstone" />). This is the default behavior, use the
    ///     <see cref="UseLegacyNullMessageHandling" /> or <see cref="SkipNullMessages" /> methods to change it.
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder HandleTombstoneMessages()
    {
        _nullMessageHandling = NullMessageHandlingStrategy.Tombstone;
        return This;
    }

    /// <summary>
    ///     Specifies that the null messages have to be silently skipped (<see cref="NullMessageHandlingStrategy.Skip" />).
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder SkipNullMessages()
    {
        _nullMessageHandling = NullMessageHandlingStrategy.Skip;
        return This;
    }

    /// <summary>
    ///     Specifies that the null messages have to be forwarded as <c>null</c> (<see cref="NullMessageHandlingStrategy.Legacy" />).
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder UseLegacyNullMessageHandling()
    {
        _nullMessageHandling = NullMessageHandlingStrategy.Legacy;
        return This;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.Build" />
    public sealed override TConfiguration Build()
    {
        TConfiguration endpoint = base.Build();

        return endpoint with
        {
            ErrorPolicy = _errorPolicy ?? endpoint.ErrorPolicy,
            ExactlyOnceStrategy = _exactlyOnceStrategy,
            Batch = _batchSize == null
                ? endpoint.Batch
                : new BatchSettings
                {
                    Size = _batchSize.Value,
                    MaxWaitTime = _batchMaxWaitTime
                },
            Sequence = endpoint.Sequence with
            {
                Timeout = _sequenceTimeout ?? endpoint.Sequence.Timeout
            },
            ThrowIfUnhandled = _throwIfUnhandled ?? endpoint.ThrowIfUnhandled,
            NullMessageHandlingStrategy = _nullMessageHandling ?? endpoint.NullMessageHandlingStrategy
        };
    }
}
