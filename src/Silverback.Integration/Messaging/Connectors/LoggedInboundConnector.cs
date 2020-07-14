// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Uses an <see cref="IInboundLog" /> to keep track of each processed message and guarantee that each
    ///     one is processed only once.
    /// </summary>
    public class LoggedInboundConnector : ExactlyOnceInboundConnector
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LoggedInboundConnector" /> class.
        /// </summary>
        /// <param name="brokerCollection">
        ///     The collection containing the available brokers.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        public LoggedInboundConnector(
            IBrokerCollection brokerCollection,
            IServiceProvider serviceProvider,
            ILogger<LoggedInboundConnector> logger)
            : base(brokerCollection, serviceProvider, logger)
        {
        }

        /// <inheritdoc cref="ExactlyOnceInboundConnector.MustProcess" />
        protected override async Task<bool> MustProcess(IRawInboundEnvelope envelope, IServiceProvider serviceProvider)
        {
            var inboundLog = serviceProvider.GetRequiredService<IInboundLog>();

            if (await inboundLog.Exists(envelope).ConfigureAwait(false))
                return false;

            serviceProvider.GetRequiredService<ConsumerTransactionManager>().Enlist(inboundLog);

            await inboundLog.Add(envelope).ConfigureAwait(false);
            return true;
        }
    }
}
