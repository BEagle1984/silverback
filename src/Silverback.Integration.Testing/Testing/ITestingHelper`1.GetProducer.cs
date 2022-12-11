// Copyright (c) 2023 Sergio Aquilini
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
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    IProducer GetProducerForEndpoint(string endpointName);
}
