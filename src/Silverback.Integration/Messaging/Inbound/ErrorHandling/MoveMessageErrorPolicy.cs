// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <summary>
    ///     This policy moves the message that failed to be processed to the configured endpoint.
    /// </summary>
    /// <remarks>
    ///     This policy can be used also to move the message at the end of the current topic to retry it later on.
    ///     The number of retries can be limited using <see cref="RetryableErrorPolicyBase.MaxFailedAttempts" />.
    /// </remarks>
    public class MoveMessageErrorPolicy : RetryableErrorPolicyBase
    {
        private Action<IOutboundEnvelope, Exception>? _transformationAction;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MoveMessageErrorPolicy" /> class.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint to move the message to.
        /// </param>
        public MoveMessageErrorPolicy(IProducerEndpoint endpoint)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            endpoint.Validate();

            Endpoint = endpoint;
        }

        internal IProducerEndpoint Endpoint { get; }

        /// <summary>
        ///     Defines an <see cref="Action{T}" /> to be called to modify (or completely rewrite) the message being
        ///     moved.
        /// </summary>
        /// <param name="transformationAction">
        ///     The <see cref="Action{T}" /> to be called to modify the message. This function can be used to modify
        ///     or replace the message body and its headers.
        /// </param>
        /// <returns>
        ///     The <see cref="MoveMessageErrorPolicy" /> so that additional calls can be chained.
        /// </returns>
        public MoveMessageErrorPolicy Transform(Action<IOutboundEnvelope, Exception> transformationAction)
        {
            _transformationAction = transformationAction;
            return this;
        }

        /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
        protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
            new MoveMessageErrorPolicyImplementation(
                Endpoint,
                _transformationAction,
                MaxFailedAttemptsCount,
                ExcludedExceptions,
                IncludedExceptions,
                ApplyRule,
                MessageToPublishFactory,
                serviceProvider
                    .GetRequiredService<IBrokerOutboundMessageEnrichersFactory>(),
                serviceProvider,
                serviceProvider
                    .GetRequiredService<IInboundLogger<MoveMessageErrorPolicy>>());

        private sealed class MoveMessageErrorPolicyImplementation : ErrorPolicyImplementation
        {
            private readonly IProducerEndpoint _endpoint;

            private readonly Action<IOutboundEnvelope, Exception>? _transformationAction;

            private readonly IBrokerOutboundMessageEnrichersFactory _enricherFactory;

            private readonly IInboundLogger<MoveMessageErrorPolicy> _logger;

            private readonly IProducer _producer;

            public MoveMessageErrorPolicyImplementation(
                IProducerEndpoint endpoint,
                Action<IOutboundEnvelope, Exception>? transformationAction,
                int? maxFailedAttempts,
                ICollection<Type> excludedExceptions,
                ICollection<Type> includedExceptions,
                Func<IRawInboundEnvelope, Exception, bool>? applyRule,
                Func<IRawInboundEnvelope, object>? messageToPublishFactory,
                IBrokerOutboundMessageEnrichersFactory enricherFactory,
                IServiceProvider serviceProvider,
                IInboundLogger<MoveMessageErrorPolicy> logger)
                : base(
                    maxFailedAttempts,
                    excludedExceptions,
                    includedExceptions,
                    applyRule,
                    messageToPublishFactory,
                    serviceProvider,
                    logger)
            {
                _endpoint = Check.NotNull(endpoint, nameof(endpoint));
                _transformationAction = transformationAction;
                _enricherFactory = enricherFactory;
                _logger = logger;

                _producer = serviceProvider.GetRequiredService<IBrokerCollection>().GetProducer(endpoint);
            }

            public override bool CanHandle(ConsumerPipelineContext context, Exception exception)
            {
                Check.NotNull(context, nameof(context));

                if (context.Sequence == null)
                    return base.CanHandle(context, exception);
                _logger.LogCannotMoveSequences(context.Envelope, context.Sequence);
                return false;
            }

            protected override async Task<bool> ApplyPolicyAsync(
                ConsumerPipelineContext context,
                Exception exception)
            {
                Check.NotNull(context, nameof(context));
                Check.NotNull(exception, nameof(exception));

                _logger.LogMoved(context.Envelope, _endpoint);

                await PublishToNewEndpointAsync(context.Envelope, exception).ConfigureAwait(false);

                await context.TransactionManager.RollbackAsync(exception, true).ConfigureAwait(false);

                return true;
            }

            private async Task PublishToNewEndpointAsync(IRawInboundEnvelope envelope, Exception exception)
            {
                var outboundEnvelope =
                    envelope is IInboundEnvelope deserializedEnvelope
                        ? new OutboundEnvelope(
                            deserializedEnvelope.Message,
                            deserializedEnvelope.Headers,
                            _endpoint)
                        : new OutboundEnvelope(envelope.RawMessage, envelope.Headers, _endpoint);

                foreach (var enricher in _enricherFactory.GetMovePolicyEnrichers(envelope.Endpoint))
                {
                    enricher.Enrich(envelope, outboundEnvelope, exception);
                }

                _transformationAction?.Invoke(outboundEnvelope, exception);

                await _producer.ProduceAsync(outboundEnvelope).ConfigureAwait(false);
            }
        }
    }
}
