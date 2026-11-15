// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Filter;

/// <summary>
///     Stands for an <see cref="IOutboundMessageFilter" /> of the specified type, which the <see cref="FilterProducerBehavior" /> resolves
///     from the producer pipeline's <see cref="IServiceProvider" /> for each message.
/// </summary>
/// <param name="FilterType">
///     The type of the <see cref="IOutboundMessageFilter" /> to be resolved.
/// </param>
internal sealed record ServiceProviderOutboundMessageFilter(Type FilterType) : IOutboundMessageFilter
{
    public IOutboundMessageFilter Resolve(IServiceProvider serviceProvider)
    {
        Check.NotNull(serviceProvider, nameof(serviceProvider));

        return (IOutboundMessageFilter)serviceProvider.GetRequiredService(FilterType);
    }

    public ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            $"The filter of type {FilterType.FullName} must be resolved from the service provider before it can be invoked.");
}
