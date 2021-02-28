// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     The base class for the builders of the types inheriting from <see cref="ConsumerEndpoint" />.
    /// </summary>
    /// <typeparam name="TEndpoint">
    ///     The type of the endpoint being built.
    /// </typeparam>
    /// <typeparam name="TBuilder">
    ///     The actual builder type.
    /// </typeparam>
    public abstract class ConsumerEndpointBuilder<TEndpoint, TBuilder>
        : EndpointBuilder<TEndpoint, TBuilder>, IConsumerEndpointBuilder<TBuilder>
        where TEndpoint : ConsumerEndpoint
        where TBuilder : IConsumerEndpointBuilder<TBuilder>
    {
        private IErrorPolicy? _errorPolicy;

        private IExactlyOnceStrategy? _exactlyOnceStrategy;

        private int? _batchSize;

        private TimeSpan? _batchMaxWaitTime;

        private TimeSpan? _sequenceTimeout;

        private bool? _throwIfUnhandled;

        private NullMessageHandlingStrategy? _nullMessageHandling;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerEndpointBuilder{TEndpoint,TBuilder}" /> class.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The optional reference to the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the
        ///     builder.
        /// </param>
        protected ConsumerEndpointBuilder(IEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
            : base(endpointsConfigurationBuilder)
        {
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.DeserializeUsing" />
        public TBuilder DeserializeUsing(IMessageSerializer serializer) =>
            UseSerializer(Check.NotNull(serializer, nameof(serializer)));

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.Decrypt" />
        public TBuilder Decrypt(EncryptionSettings encryptionSettings) =>
            WithEncryption(Check.NotNull(encryptionSettings, nameof(encryptionSettings)));

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.OnError(IErrorPolicy)" />
        public TBuilder OnError(IErrorPolicy errorPolicy)
        {
            _errorPolicy = Check.NotNull(errorPolicy, nameof(errorPolicy));
            return This;
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.OnError(Action{IErrorPolicyBuilder})" />
        public TBuilder OnError(Action<IErrorPolicyBuilder> errorPolicyBuilderAction)
        {
            Check.NotNull(errorPolicyBuilderAction, nameof(errorPolicyBuilderAction));

            var errorPolicyBuilder = new ErrorPolicyBuilder(EndpointsConfigurationBuilder);
            errorPolicyBuilderAction.Invoke(errorPolicyBuilder);

            return OnError(errorPolicyBuilder.Build());
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.EnsureExactlyOnce(IExactlyOnceStrategy)" />
        public TBuilder EnsureExactlyOnce(IExactlyOnceStrategy strategy)
        {
            _exactlyOnceStrategy = Check.NotNull(strategy, nameof(strategy));
            return This;
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.EnsureExactlyOnce(Action{IExactlyOnceStrategyBuilder})" />
        public TBuilder EnsureExactlyOnce(Action<IExactlyOnceStrategyBuilder> strategyBuilderAction)
        {
            Check.NotNull(strategyBuilderAction, nameof(strategyBuilderAction));

            var strategyBuilder = new ExactlyOnceStrategyBuilder();
            strategyBuilderAction.Invoke(strategyBuilder);

            return EnsureExactlyOnce(strategyBuilder.Build());
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.EnableBatchProcessing(int, TimeSpan?)" />
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

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.WithSequenceTimeout(TimeSpan)" />
        public TBuilder WithSequenceTimeout(TimeSpan timeout)
        {
            _sequenceTimeout = timeout;
            return This;
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.ThrowIfUnhandled" />
        public TBuilder ThrowIfUnhandled()
        {
            _throwIfUnhandled = true;
            return This;
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.IgnoreUnhandledMessages" />
        public TBuilder IgnoreUnhandledMessages()
        {
            _throwIfUnhandled = false;
            return This;
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.HandleTombstoneMessages" />
        public TBuilder HandleTombstoneMessages()
        {
            _nullMessageHandling = NullMessageHandlingStrategy.Tombstone;
            return This;
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.SkipNullMessages" />
        public TBuilder SkipNullMessages()
        {
            _nullMessageHandling = NullMessageHandlingStrategy.Skip;
            return This;
        }

        /// <inheritdoc cref="IConsumerEndpointBuilder{TBuilder}.UseLegacyNullMessageHandling" />
        public TBuilder UseLegacyNullMessageHandling()
        {
            _nullMessageHandling = NullMessageHandlingStrategy.Legacy;
            return This;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.Build" />
        public override TEndpoint Build()
        {
            var endpoint = base.Build();

            if (_errorPolicy != null)
                endpoint.ErrorPolicy = _errorPolicy;

            if (_exactlyOnceStrategy != null)
                endpoint.ExactlyOnceStrategy = _exactlyOnceStrategy;

            if (_batchSize != null)
            {
                endpoint.Batch = new BatchSettings
                {
                    Size = _batchSize.Value,
                    MaxWaitTime = _batchMaxWaitTime
                };
            }

            if (_sequenceTimeout != null)
                endpoint.Sequence.Timeout = _sequenceTimeout.Value;

            if (_throwIfUnhandled != null)
                endpoint.ThrowIfUnhandled = _throwIfUnhandled.Value;

            if (_nullMessageHandling != null)
                endpoint.NullMessageHandlingStrategy = _nullMessageHandling.Value;

            return endpoint;
        }
    }
}
