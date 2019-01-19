// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Uses <see cref="IInboundLog"/> to keep track of each processed message and guarantee
    /// that each one is processed only once.
    /// </summary>
    public class LoggedInboundConnector : ExactlyOnceInboundConnector
    {
        public LoggedInboundConnector(IBroker broker, IServiceProvider serviceProvider, ILogger<LoggedInboundConnector> logger, MessageLogger messageLogger)
            : base(broker, serviceProvider, logger, messageLogger)
        {
        }

        protected override bool MustProcess(object message, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            var inboundLog = serviceProvider.GetRequiredService<IInboundLog>();

            if (inboundLog.Exists(message, sourceEndpoint))
                return false;

            inboundLog.Add(message, sourceEndpoint);
            return true;
        }

        protected override void Commit(IServiceProvider serviceProvider) =>
            serviceProvider.GetRequiredService<IInboundLog>().Commit();

        protected override void Rollback(IServiceProvider serviceProvider) =>
            serviceProvider.GetRequiredService<IInboundLog>().Rollback();
    }
}