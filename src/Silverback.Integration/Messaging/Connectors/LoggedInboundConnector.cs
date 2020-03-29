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
    ///     Uses <see cref="IInboundLog" /> to keep track of each processed message and guarantee
    ///     that each one is processed only once.
    /// </summary>
    public class LoggedInboundConnector : ExactlyOnceInboundConnector
    {
        public LoggedInboundConnector(
            IBrokerCollection brokerCollection,
            IServiceProvider serviceProvider,
            ILogger<LoggedInboundConnector> logger,
            MessageLogger messageLogger)
            : base(brokerCollection, serviceProvider, logger, messageLogger)
        {
        }

        protected override async Task<bool> MustProcess(IRawInboundEnvelope envelope, IServiceProvider serviceProvider)
        {
            var inboundLog = serviceProvider.GetRequiredService<IInboundLog>();

            if (!(envelope is IInboundEnvelope deserializedEnvelope) || deserializedEnvelope.Message == null)
                return true;

            if (await inboundLog.Exists(deserializedEnvelope.Message, envelope.Endpoint))
                return false;

            serviceProvider.GetRequiredService<ConsumerTransactionManager>().Enlist(inboundLog);

            await inboundLog.Add(deserializedEnvelope.Message, envelope.Endpoint);
            return true;
        }
    }
}