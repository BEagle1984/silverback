// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Testing;

/// <content>
///     Declares the <c>GetConsumer</c> methods.
/// </content>
public partial interface ITestingHelper
{
    /// <summary>
    ///     Gets the consumer with the specified name.
    /// </summary>
    /// <param name="name">
    ///     The consumer name.
    /// </param>
    /// <returns>
    ///     The <see cref="IConsumer" /> with the specified name.
    /// </returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">
    ///     No consumer with the specified <paramref name="name" /> exists.
    /// </exception>
    IConsumer GetConsumer(string name);

    /// <summary>
    ///     Gets the existing consumer connected to specified endpoint.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">
    ///     No consumer is configured for the specified endpoint.
    /// </exception>
    IConsumer GetConsumerForEndpoint(string endpointName);
}
