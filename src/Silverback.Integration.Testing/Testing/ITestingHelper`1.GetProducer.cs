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
    IProducer GetProducerForEndpoint(string endpointName);
}
