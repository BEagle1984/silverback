// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Routing;

/// <summary>
///     Produces the <see cref="IOutboundEnvelope{TMessage}" /> using the <see cref="IProduceStrategy" />
///     configured in the endpoint.
/// </summary>
public class ProduceBehavior : IBehavior, ISorted
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProduceBehavior" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to build the <see cref="IProduceStrategyImplementation" />.
    /// </param>
    public ProduceBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => IntegrationBehaviorsSortIndexes.OutboundProducer;

    /// <inheritdoc cref="IBehavior.HandleAsync" />
    public async ValueTask<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next)
    {
        Check.NotNull(next, nameof(next));

        if (message is IOutboundEnvelope envelope)
        {
            IProduceStrategyImplementation produceStrategy =
                envelope.Endpoint.Configuration.Strategy.Build(_serviceProvider, envelope.Endpoint.Configuration);

            switch (message)
            {
                case IOutboundEnvelope { Message: IAsyncEnumerable<object> messages }:
                    await produceStrategy.ProduceAsync(
                        messages.Select(
                            enumeratedMessage =>
                                OutboundEnvelopeFactory.CreateEnvelope(enumeratedMessage, envelope))).ConfigureAwait(false);
                    break;
                case IOutboundEnvelope { Message: IEnumerable<object> messages }:
                    await produceStrategy.ProduceAsync(
                        messages.Select(
                            enumeratedMessage =>
                                OutboundEnvelopeFactory.CreateEnvelope(enumeratedMessage, envelope))).ConfigureAwait(false);
                    break;
                default:
                    await produceStrategy.ProduceAsync(envelope).ConfigureAwait(false);
                    break;
            }
        }

        return await next(message).ConfigureAwait(false);
    }
}
