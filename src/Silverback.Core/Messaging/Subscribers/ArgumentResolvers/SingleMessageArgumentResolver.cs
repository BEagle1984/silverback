// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     Resolves the parameters declared with a type that is compatible with the type of the message being
///     published.
/// </summary>
public class SingleMessageArgumentResolver : ISingleMessageArgumentResolver
{
    /// <inheritdoc cref="IArgumentResolver.CanResolve" />
    public bool CanResolve(Type parameterType) => true;

    /// <inheritdoc cref="IMessageArgumentResolver.GetMessageType" />
    public Type GetMessageType(Type parameterType) => parameterType;

    /// <inheritdoc cref="ISingleMessageArgumentResolver.GetValue" />
    public object? GetValue(object? message, Type parameterType) => message;
}
