// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Validation;

/// <summary>
///     Determines whether the message is valid.
/// </summary>
public class ValidatorProducerBehavior : IProducerBehavior
{
    private readonly IProducerLogger<ValidatorProducerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidatorProducerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IProducerLogger{TCategoryName}" />.
    /// </param>
    public ValidatorProducerBehavior(IProducerLogger<ValidatorProducerBehavior> logger)
    {
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Validator;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope.Message != null &&
            context.Envelope.Endpoint.Configuration.MessageValidationMode != MessageValidationMode.None &&
            !MessageValidator.IsValid(
                context.Envelope.Message,
                context.Envelope.Endpoint.Configuration.MessageValidationMode,
                out string? validationErrors))
        {
            _logger.LogInvalidMessage(context.Envelope, validationErrors);
        }

        await next(context).ConfigureAwait(false);
    }
}
