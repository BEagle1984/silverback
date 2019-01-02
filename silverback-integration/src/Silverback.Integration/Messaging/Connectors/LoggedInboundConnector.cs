// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Uses <see cref="IInboundLog"/> to keep track of each processed message and guarantee
    /// that each one is processed only once.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Connectors.ExactlyOnceInboundConnector" />
    public class LoggedInboundConnector : ExactlyOnceInboundConnector
    {
        public LoggedInboundConnector(IBroker broker, IServiceProvider serviceProvider, ILogger<LoggedInboundConnector> logger)
            : base(broker, serviceProvider, logger)
        {
        }


        protected override bool MustProcess(IMessage message, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            if (!(message is IIntegrationMessage integrationMessage))
            {
                throw new NotSupportedException("The LoggedInboundConnector currently supports only instances of IIntegrationMessage.");
            }

            var inboundLog = serviceProvider.GetRequiredService<IInboundLog>();

            if (inboundLog.Exists(integrationMessage, sourceEndpoint))
                return false;

            inboundLog.Add(integrationMessage, sourceEndpoint);
            return true;
        }

        protected override void CommitBatch(IServiceProvider serviceProvider) =>
            serviceProvider.GetRequiredService<IInboundLog>().Commit();

        protected override void RollbackBatch(IServiceProvider serviceProvider) =>
            serviceProvider.GetRequiredService<IInboundLog>().Rollback();
    }
}