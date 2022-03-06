﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Headers;

/// <summary>
///     Applies the custom header name mappings.
/// </summary>
public class CustomHeadersMapperProducerBehavior : IProducerBehavior
{
    private readonly ICustomHeadersMappings? _mappings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CustomHeadersMapperProducerBehavior" /> class.
    /// </summary>
    /// <param name="mappings">
    ///     The <see cref="ICustomHeadersMappings" /> containing the mappings to be applied.
    /// </param>
    public CustomHeadersMapperProducerBehavior(ICustomHeadersMappings? mappings)
    {
        _mappings = mappings;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.CustomHeadersMapper;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        _mappings?.Apply(context.Envelope.Headers);

        await next(context).ConfigureAwait(false);
    }
}
