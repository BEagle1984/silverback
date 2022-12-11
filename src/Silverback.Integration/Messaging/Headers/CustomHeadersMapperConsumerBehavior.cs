// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Headers;

/// <summary>
///     Applies the custom header name mappings.
/// </summary>
public class CustomHeadersMapperConsumerBehavior : IConsumerBehavior
{
    private readonly ICustomHeadersMappings? _mappings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CustomHeadersMapperConsumerBehavior" /> class.
    /// </summary>
    /// <param name="mappings">
    ///     The <see cref="ICustomHeadersMappings" /> containing the mappings to be applied.
    /// </param>
    public CustomHeadersMapperConsumerBehavior(ICustomHeadersMappings? mappings)
    {
        _mappings = mappings;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.CustomHeadersMapper;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (_mappings is { Count: > 0 })
            _mappings.Revert(context.Envelope.Headers);

        await next(context).ConfigureAwait(false);
    }
}
