// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Validation;

/// <summary>
///     Deserializes the messages being consumed using the configured <see cref="IMessageSerializer" />.
/// </summary>
public class ValidatorConsumerBehavior : IConsumerBehavior
{
    private readonly IConsumerLogger<ValidatorConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidatorConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IConsumerLogger{TCategoryName}" />.
    /// </param>
    public ValidatorConsumerBehavior(IConsumerLogger<ValidatorConsumerBehavior> logger)
    {
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Validator;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope.Endpoint.Configuration.MessageValidationMode != MessageValidationMode.None &&
            context.Envelope is IInboundEnvelope { Message: not null } deserializedEnvelope &&
            !MessageValidator.IsValid(
                deserializedEnvelope.Message,
                context.Envelope.Endpoint.Configuration.MessageValidationMode,
                out string? validationErrors))
        {
            _logger.LogInvalidMessage(context.Envelope, validationErrors);
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
