// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ErrorHandling;

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
    /// <param name="producerConfiguration">
    ///     The configuration of the producer to be used to move the message.
    /// </param>
    public MoveMessageErrorPolicy(ProducerConfiguration producerConfiguration)
    {
        Check.NotNull(producerConfiguration, nameof(producerConfiguration));

        producerConfiguration.Validate();

        ProducerConfiguration = producerConfiguration;
    }

    internal ProducerConfiguration ProducerConfiguration { get; }

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
            ProducerConfiguration,
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
        private readonly ProducerConfiguration _producerConfiguration;

        private readonly Action<IOutboundEnvelope, Exception>? _transformationAction;

        private readonly IInboundLogger<MoveMessageErrorPolicy> _logger;

        private readonly IBrokerOutboundMessageEnrichersFactory _enricherFactory;

        private readonly SemaphoreSlim _producerInitSemaphore = new(1, 1);

        private IProducer? _producer;

        public MoveMessageErrorPolicyImplementation(
            ProducerConfiguration producerConfiguration,
            Action<IOutboundEnvelope, Exception>? transformationAction,
            int? maxFailedAttempts,
            ICollection<Type> excludedExceptions,
            ICollection<Type> includedExceptions,
            Func<IRawInboundEnvelope, Exception, bool>? applyRule,
            Func<IRawInboundEnvelope, Exception, object?>? messageToPublishFactory,
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
            _producerConfiguration = Check.NotNull(producerConfiguration, nameof(producerConfiguration));
            _transformationAction = transformationAction;
            _enricherFactory = enricherFactory;
            _logger = logger;
        }

        public override bool CanHandle(ConsumerPipelineContext context, Exception exception)
        {
            Check.NotNull(context, nameof(context));

            if (context.Sequence != null)
            {
                _logger.LogCannotMoveSequences(context.Envelope, context.Sequence);
                return false;
            }

            return base.CanHandle(context, exception);
        }

        protected override async Task<bool> ApplyPolicyAsync(ConsumerPipelineContext context, Exception exception)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(exception, nameof(exception));

            _logger.LogMoved(context.Envelope, _producerConfiguration);

            await PublishToNewEndpointAsync(context.Envelope, context.ServiceProvider, exception)
                .ConfigureAwait(false);

            await context.TransactionManager.RollbackAsync(exception, true).ConfigureAwait(false);

            return true;
        }

        private async Task PublishToNewEndpointAsync(IRawInboundEnvelope envelope, IServiceProvider serviceProvider, Exception exception)
        {
            // TODO: Use OutboundEnvelopeFactory
            OutboundEnvelope outboundEnvelope =
                envelope is IInboundEnvelope deserializedEnvelope
                    ? new OutboundEnvelope(
                        deserializedEnvelope.Message,
                        deserializedEnvelope.Headers,
                        _producerConfiguration.Endpoint.GetEndpoint(deserializedEnvelope.Message, _producerConfiguration, serviceProvider))
                    : new OutboundEnvelope(
                        envelope.RawMessage,
                        envelope.Headers,
                        _producerConfiguration.Endpoint.GetEndpoint(envelope.RawMessage, _producerConfiguration, serviceProvider));

            IMovePolicyMessageEnricher enricher = _enricherFactory.GetMovePolicyEnricher(envelope.Endpoint);
            enricher.Enrich(envelope, outboundEnvelope, exception);

            _transformationAction?.Invoke(outboundEnvelope, exception);

            _producer ??= await InitProducerAsync(serviceProvider).ConfigureAwait(false);

            await _producer.ProduceAsync(outboundEnvelope).ConfigureAwait(false);
        }

        private async Task<IProducer> InitProducerAsync(IServiceProvider serviceProvider)
        {
            await _producerInitSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                IBrokerCollection brokerCollection = serviceProvider.GetRequiredService<IBrokerCollection>();
                _producer = await brokerCollection.GetProducerAsync(_producerConfiguration).ConfigureAwait(false);
                return _producer;
            }
            finally
            {
                _producerInitSemaphore.Release();
            }
        }
    }
}
