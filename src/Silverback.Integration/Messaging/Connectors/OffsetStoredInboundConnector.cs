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
    public class OffsetStoredInboundConnector : ExactlyOnceInboundConnector
    {
        public OffsetStoredInboundConnector(
            IBroker broker,
            IServiceProvider serviceProvider,
            ILogger<OffsetStoredInboundConnector> logger,
            MessageLogger messageLogger)
            : base(broker, serviceProvider, logger, messageLogger)
        {
        }

        protected override async Task<bool> MustProcess(IInboundEnvelope envelope, IServiceProvider serviceProvider)
        {
            if (envelope.Offset == null || !(envelope.Offset is IComparableOffset comparableOffset))
                throw new InvalidOperationException(
                    "The message broker implementation doesn't seem to support comparable offsets. " +
                    "The OffsetStoredInboundConnector cannot be used, please resort to LoggedInboundConnector " +
                    "to ensure exactly-once delivery.");

            var offsetStore = serviceProvider.GetRequiredService<IOffsetStore>();

            var latest = await offsetStore.GetLatestValue(envelope.Offset.Key, envelope.Endpoint);
            if (latest != null && latest.CompareTo(comparableOffset) >= 0)
                return false;

            await offsetStore.Store(comparableOffset, envelope.Endpoint);
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