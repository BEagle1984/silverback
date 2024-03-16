// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps a message when it's being transferred over a message broker.
/// </summary>
public interface IEnvelope : IMessageWrapper
{
    /// <summary>
    ///     Gets a value indicating whether the contained message can be automatically unwrapped forwarded to the matching subscribers
    ///     in its pure form.
    /// </summary>
    /// <remarks>
    ///     This is internally used to avoid mortal loops.
    /// </remarks>
    bool AutoUnwrap { get; }
}
