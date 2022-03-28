// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Holds a reference to all the configured <see cref="IConsumer" />.
/// </summary>
public interface IConsumerCollection : IReadOnlyList<IConsumer>
{
    // /// <summary>
    // ///     Returns the consumer with the specified id.
    // /// </summary>
    // IConsumer Get(string id);
    //
    // /// <summary>
    // ///     Returns the consumers that consume the specified endpoint (topic or queue).
    // /// </summary>
    // /// <remarks>
    // ///     The <paramref name="endpointName"/> can be either the raw endpoint name or the given friendly name.
    // /// </remarks>
    // /// <param name="endpointName"></param>
    // /// <returns></returns>
    // IReadOnlyCollection<IConsumer> FindByEndpoint(string endpointName);
    //
    // /// <summary>
    // ///     Returns the first consumer that consumes the specified endpoint (topic or queue).
    // /// </summary>
    // /// <remarks>
    // ///     The <paramref name="endpointName"/> can be either the raw endpoint name or the given friendly name.
    // /// </remarks>
    // /// <param name="endpointName"></param>
    // /// <returns></returns>
    // IConsumer GetByEndpoint(string endpointName);
}
