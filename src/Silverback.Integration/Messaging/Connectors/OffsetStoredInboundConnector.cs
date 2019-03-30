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
    public class OffsetStoredInboundConnector: ExactlyOnceInboundConnector
    {
        public OffsetStoredInboundConnector(IBroker broker, IServiceProvider serviceProvider,
            ILogger<OffsetStoredInboundConnector> logger, MessageLogger messageLogger) 
            : base(broker, serviceProvider, logger, messageLogger)
        {
        }

        protected override bool MustProcess(MessageReceivedEventArgs messageArgs, IEndpoint sourceEndpoint, IServiceProvider serviceProvider)
        {
            var offsetStore = serviceProvider.GetRequiredService<IOffsetStore>();

            var latest = offsetStore.GetLatestValue(messageArgs.Offset.Key);
            if (latest != null && messageArgs.Offset.CompareTo(latest) <= 0)
                return false;

            offsetStore.Store(messageArgs.Offset);
            return true;
        }

        protected override void Commit(IServiceProvider serviceProvider)
        {
            base.Commit(serviceProvider);
            serviceProvider.GetRequiredService<IOffsetStore>().Commit();
        }

        protected override void Rollback(IServiceProvider serviceProvider)
        {
            base.Rollback(serviceProvider);
            serviceProvider.GetRequiredService<IOffsetStore>().Rollback();
        }
    }
}