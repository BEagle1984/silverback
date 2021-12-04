// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Inbound.ErrorHandling;

/// <summary>
///     An error policy is used to handle errors that may occur while processing the inbound messages.
/// </summary>
public interface IErrorPolicy
{
    /// <summary>
    ///     Returns the actual error policy implementation, built using the provided
    ///     <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to build the error policy.
    /// </param>
    /// <returns>
    ///     An instance of <see cref="IErrorPolicyImplementation" /> that can be used to handle the processing
    ///     error.
    /// </returns>
    IErrorPolicyImplementation Build(IServiceProvider serviceProvider);
}
