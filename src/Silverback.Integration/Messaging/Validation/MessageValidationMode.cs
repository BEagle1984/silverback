// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Validation;

/// <summary>
///     Specifies the message validation mode.
/// </summary>
public enum MessageValidationMode
{
    /// <summary>
    ///     No validation is performed.
    /// </summary>
    None,

    /// <summary>
    ///     A warning is logged if the message is not valid.
    /// </summary>
    LogWarning,

    /// <summary>
    ///     An exception is thrown if the message is not valid.
    /// </summary>
    ThrowException
}
