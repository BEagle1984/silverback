// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.Filter;

/// <summary>
///     Stands for an <see cref="IInboundMessageFilter" /> of the specified type, which the <see cref="FilterConsumerBehavior" /> resolves
///     from the service provider of each consumed message.
/// </summary>
/// <param name="FilterType">
///     The type of the <see cref="IInboundMessageFilter" /> to be resolved.
/// </param>
internal sealed record ServiceProviderInboundMessageFilter(Type FilterType) : IInboundMessageFilter
{
    public IInboundMessageFilter Resolve(IServiceProvider serviceProvider)
    {
        Check.NotNull(serviceProvider);

        return (IInboundMessageFilter)serviceProvider.GetRequiredService(FilterType);
    }

    public ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            $"The filter of type {FilterType.FullName} must be resolved from the service provider before it can be invoked.");
}
