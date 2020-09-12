// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound
{
    /// <inheritdoc cref="IOutboundConnector" />
    public class OutboundConnector : IOutboundConnector
    {
        private readonly IBrokerCollection _brokerCollection;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundConnector" /> class.
        /// </summary>
        /// <param name="brokerCollection">
        ///     The collection of <see cref="IBroker" />.
        /// </param>
        public OutboundConnector(IBrokerCollection brokerCollection)
        {
            _brokerCollection = brokerCollection;
        }

        /// <inheritdoc cref="IOutboundConnector.RelayMessage" />
        public Task RelayMessage(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return _brokerCollection.GetProducer(envelope.Endpoint).ProduceAsync(envelope);
        }
    }
}
