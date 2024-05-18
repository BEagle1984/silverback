// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <summary>
///     This policy moves the message that failed to be processed to the configured endpoint.
/// </summary>
/// <remarks>
///     This policy can be used also to move the message at the end of the current topic to retry it later on.
///     The number of retries can be limited using <see cref="ErrorPolicyBase.MaxFailedAttempts" />.
/// </remarks>
public record MoveMessageErrorPolicy : ErrorPolicyBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MoveMessageErrorPolicy" /> class.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name of an endpoint that is already configured with
    ///     a producer.
    /// </param>
    internal MoveMessageErrorPolicy(string endpointName)
    {
        EndpointName = Check.NotNull(endpointName, nameof(endpointName));
    }

    /// <summary>
    ///     Gets the target endpoint name. It could be either the topic/queue name or the friendly name of an endpoint that is already
    ///     configured with a producer.
    /// </summary>
    public string EndpointName { get; }

    /// <summary>
    ///     Gets an <see cref="Action{T1,T2}" /> to be used to modify the message before moving it to the target endpoint.
    /// </summary>
    public Action<IOutboundEnvelope, Exception>? TransformMessageAction { get; init; }

    /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
    protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
        new MoveMessageErrorPolicyImplementation(
            EndpointName,
            serviceProvider.GetRequiredService<IProducerCollection>(),
            TransformMessageAction,
            MaxFailedAttempts,
            ExcludedExceptions,
            IncludedExceptions,
            ApplyRule,
            MessageToPublishFactory,
            serviceProvider.GetRequiredService<IBrokerOutboundMessageEnrichersFactory>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IConsumerLogger<MoveMessageErrorPolicy>>());

    private sealed class MoveMessageErrorPolicyImplementation : ErrorPolicyImplementation
    {
        private readonly string _endpointName;

        private readonly IProducerCollection _producers;

        private readonly Action<IOutboundEnvelope, Exception>? _transformationAction;

        private readonly IConsumerLogger<MoveMessageErrorPolicy> _logger;

        private readonly IBrokerOutboundMessageEnrichersFactory _enricherFactory;

        private IProducer? _producer;

        public MoveMessageErrorPolicyImplementation(
            string endpointName,
            IProducerCollection producers,
            Action<IOutboundEnvelope, Exception>? transformationAction,
            int? maxFailedAttempts,
            IReadOnlyCollection<Type> excludedExceptions,
            IReadOnlyCollection<Type> includedExceptions,
            Func<IRawInboundEnvelope, Exception, bool>? applyRule,
            Func<IRawInboundEnvelope, Exception, object?>? messageToPublishFactory,
            IBrokerOutboundMessageEnrichersFactory enricherFactory,
            IServiceProvider serviceProvider,
            IConsumerLogger<MoveMessageErrorPolicy> logger)
            : base(
                maxFailedAttempts,
                excludedExceptions,
                includedExceptions,
                applyRule,
                messageToPublishFactory,
                serviceProvider,
                logger)
        {
            _endpointName = endpointName;
            _producers = producers;
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

            await PublishToNewEndpointAsync(context.Envelope, context.ServiceProvider, exception).ConfigureAwait(false);

            await context.TransactionManager.RollbackAsync(exception, true).ConfigureAwait(false);

            return true;
        }

        private async ValueTask PublishToNewEndpointAsync(IRawInboundEnvelope envelope, IServiceProvider serviceProvider, Exception exception)
        {
            _producer ??= _producers.GetProducerForEndpoint(_endpointName);

            IOutboundEnvelope outboundEnvelope =
                envelope is IInboundEnvelope deserializedEnvelope
                    ? OutboundEnvelopeFactory.CreateEnvelope(
                        deserializedEnvelope.Message,
                        deserializedEnvelope.Headers,
                        _producer.EndpointConfiguration.Endpoint.GetEndpoint(
                            deserializedEnvelope.Message,
                            _producer.EndpointConfiguration,
                            serviceProvider),
                        _producer)
                    : OutboundEnvelopeFactory.CreateEnvelope(
                        envelope.RawMessage,
                        envelope.Headers,
                        _producer.EndpointConfiguration.Endpoint.GetEndpoint(
                            envelope.RawMessage,
                            _producer.EndpointConfiguration,
                            serviceProvider),
                        _producer);

            IMovePolicyMessageEnricher enricher = _enricherFactory.GetMovePolicyEnricher(envelope.Endpoint);
            enricher.Enrich(envelope, outboundEnvelope, exception);

            _transformationAction?.Invoke(outboundEnvelope, exception);

            await _producer.ProduceAsync(outboundEnvelope).ConfigureAwait(false);

            _logger.LogMoved(envelope, _producer.EndpointConfiguration);
        }
    }
}
