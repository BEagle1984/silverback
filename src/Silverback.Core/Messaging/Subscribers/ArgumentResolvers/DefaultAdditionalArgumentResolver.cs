// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     Resolves the additional parameters of the subscribed methods using the <see cref="IServiceProvider" />.
/// </summary>
public class DefaultAdditionalArgumentResolver : IAdditionalArgumentResolver
{
    /// <inheritdoc cref="IArgumentResolver.CanResolve" />
    public bool CanResolve(Type parameterType) => true;

    /// <inheritdoc cref="IAdditionalArgumentResolver.GetValue" />
    public object GetValue(Type parameterType, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        Check.NotNull(serviceProvider, nameof(serviceProvider)).GetRequiredService(parameterType);
}
