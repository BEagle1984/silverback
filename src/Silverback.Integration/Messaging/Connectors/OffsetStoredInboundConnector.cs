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
    public class OffsetStoredInboundConnector: ExactlyOnceInboundConnector
    {
        public OffsetStoredInboundConnector(IBroker broker, IServiceProvider serviceProvider,
            ILogger<OffsetStoredInboundConnector> logger, MessageLogger messageLogger) 
            : base(broker, serviceProvider, logger, messageLogger)
        {
        }

        protected override async Task<bool> MustProcess(IInboundMessage message, IServiceProvider serviceProvider)
        {
            var offsetStore = serviceProvider.GetRequiredService<IOffsetStore>();

            var latest = await offsetStore.GetLatestValue(message.Offset.Key);
            if (latest != null && message.Offset.CompareTo(latest) <= 0)
                return false;

            await offsetStore.Store(message.Offset);
            return true;
        }

        protected override async Task Commit(IServiceProvider serviceProvider)
        {
            await base.Commit(serviceProvider);
            await serviceProvider.GetRequiredService<IOffsetStore>().Commit();
        }

        protected override async Task Rollback(IServiceProvider serviceProvider)
        {
            await base.Rollback(serviceProvider);
            await serviceProvider.GetRequiredService<IOffsetStore>().Rollback();
        }
    }
}