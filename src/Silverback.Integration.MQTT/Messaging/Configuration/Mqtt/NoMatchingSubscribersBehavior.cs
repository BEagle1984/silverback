// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Defines the behavior when no matching subscribers are found for the produced message.
/// </summary>
public enum NoMatchingSubscribersBehavior
{
    /// <summary>
    ///     No exception is thrown when no matching subscribers are found for the produced message. This is the default behavior.
    /// </summary>
    Ignore,

    /// <summary>
    ///     No exception is thrown when no matching subscribers are found for the produced message, but a warning is logged.
    /// </summary>
    LogWarning,

    /// <summary>
    ///     An exception is thrown when no matching subscribers are found for the produced message.
    /// </summary>
    Throw
}
