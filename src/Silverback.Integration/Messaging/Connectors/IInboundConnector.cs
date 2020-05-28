// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public interface IInboundConnector
    {
        /// <summary>
        ///     Binds to the specified endpoint to consume it.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="errorPolicy">
        ///     The optional error policy to be applied when an exception is thrown during the processing of a
        ///     message.
        /// </param>
        /// <param name="settings">
        ///     The additional settings such as batch consuming and number of parallel consumers.
        /// </param>
        /// <returns>
        ///     The <see cref="IInboundConnector" /> so that additional calls can be chained.
        /// </returns>
        IInboundConnector Bind(
            IConsumerEndpoint endpoint,
            IErrorPolicy? errorPolicy = null,
            InboundConnectorSettings? settings = null);
    }
}
