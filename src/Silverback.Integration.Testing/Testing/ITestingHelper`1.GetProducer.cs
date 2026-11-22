// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Testing;

/// <content>
///     Declares the <c>GetProducer</c> methods.
/// </content>
public partial interface ITestingHelper
{
    /// <summary>
    ///     Gets a producer for the specified endpoint.
    /// </summary>
    /// <remarks>
    ///     If no producer endpoint has the specified name, a new producer is created mirroring the consumer endpoint subscribing the
    ///     topic or having the specified friendly name: it serializes and encrypts the messages the way the consumer expects them and
    ///     produces them to the consumed topic. The connection security settings aren't mirrored.
    /// </remarks>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">
    ///     No producer or consumer is configured for the specified endpoint, the endpoint is a topic pattern or wildcard filter, the
    ///     consumer endpoint subscribes only topic patterns or wildcard filters, or the key provider of the consumer's decryption
    ///     settings doesn't return a key.
    /// </exception>
    /// <exception cref="System.NotSupportedException">
    ///     The decryption settings of the consumer endpoint to mirror aren't supported.
    /// </exception>
    IProducer GetProducerForEndpoint(string endpointName);
}
