// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Validation
{
    /// <summary>
    ///     Deserializes the messages being consumed using the configured <see cref="IMessageSerializer" />.
    /// </summary>
    public class ValidatorConsumerBehavior : IConsumerBehavior
    {
        private readonly IInboundLogger<ValidatorConsumerBehavior> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ValidatorConsumerBehavior" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The <see cref="IInboundLogger{TCategoryName}" />.
        /// </param>
        public ValidatorConsumerBehavior(IInboundLogger<ValidatorConsumerBehavior> logger)
        {
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Validator;

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        public async Task HandleAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope.Endpoint.MessageValidationMode != MessageValidationMode.None &&
                context.Envelope is IInboundEnvelope deserializeEnvelope &&
                deserializeEnvelope.Message != null)
            {
                (bool isValid, string? validationErrors) = MessageValidator.CheckMessageIsValid(
                    deserializeEnvelope.Message,
                    context.Envelope.Endpoint.MessageValidationMode);

                if (!isValid)
                {
                    _logger.LogInvalidMessageProcessed(validationErrors!);
                }
            }

            await next(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
