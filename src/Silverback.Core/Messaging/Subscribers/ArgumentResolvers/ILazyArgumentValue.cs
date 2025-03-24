// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     Represent a value for an argument that will be available in the future.
/// </summary>
public interface ILazyArgumentValue
{
    /// <summary>
    ///     Gets the argument value, as soon as it is available.
    /// </summary>
    object? Value { get; }

    /// <summary>
    ///     Gets an awaitable <see cref="Task" /> that completes when the argument value is available.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WaitUntilCreatedAsync();
}
