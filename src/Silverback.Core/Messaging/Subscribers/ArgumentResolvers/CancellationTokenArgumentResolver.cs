// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     Provides the <see cref="CancellationToken" /> to the subscribed method.
/// </summary>
public class CancellationTokenArgumentResolver : IAdditionalArgumentResolver
{
    /// <inheritdoc cref="IArgumentResolver.CanResolve" />
    public bool CanResolve(Type parameterType) => parameterType == typeof(CancellationToken);

    /// <inheritdoc cref="IAdditionalArgumentResolver.GetValue" />
    public object GetValue(Type parameterType, IServiceProvider serviceProvider, CancellationToken cancellationToken) => cancellationToken;
}
