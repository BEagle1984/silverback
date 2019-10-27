// Copyright (c) 2019 Sergio Aquilini
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
    /// Uses <see cref="IInboundLog"/> to keep track of each processed message and guarantee
    /// that each one is processed only once.
    /// </summary>
    public class LoggedInboundConnector : ExactlyOnceInboundConnector
    {
        public LoggedInboundConnector(IBroker broker, IServiceProvider serviceProvider,
            ILogger<LoggedInboundConnector> logger, MessageLogger messageLogger)
            : base(broker, serviceProvider, logger, messageLogger)
        {
        }

        protected override async Task<bool> MustProcess(IInboundMessage message, IServiceProvider serviceProvider)
        {
            var inboundLog = serviceProvider.GetRequiredService<IInboundLog>();

            if (await inboundLog.Exists(message.Content, message.Endpoint))
                return false;

            await inboundLog.Add(message.Content, message.Endpoint);
            return true;
        }

        protected override async Task Commit(IServiceProvider serviceProvider)
        {
            await base.Commit(serviceProvider);
            await serviceProvider.GetRequiredService<IInboundLog>().Commit();
        }

        protected override async Task Rollback(IServiceProvider serviceProvider)
        {
            await base.Rollback(serviceProvider);
            await serviceProvider.GetRequiredService<IInboundLog>().Rollback();
        }
    }
}