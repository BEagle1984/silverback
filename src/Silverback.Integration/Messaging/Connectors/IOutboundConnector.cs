// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Forwards the outbound messages to the message broker.
    /// </summary>
    public interface IOutboundConnector
    {
        /// <summary>
        ///     Forwards the message to the message broker endpoint.
        /// </summary>
        /// <param name="envelope">The envelope containing the message to be produced.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RelayMessage(IOutboundEnvelope envelope);
    }
}