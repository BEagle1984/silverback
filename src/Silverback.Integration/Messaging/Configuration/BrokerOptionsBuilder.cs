// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Outbound;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal class BrokerOptionsBuilder : IBrokerOptionsBuilder
    {
        public BrokerOptionsBuilder(ISilverbackBuilder silverbackBuilder)
        {
            SilverbackBuilder = silverbackBuilder;
        }

        public ISilverbackBuilder SilverbackBuilder { get; }

        public ILogTemplates LogTemplates =>
            SilverbackBuilder.Services.GetSingletonServiceInstance<ILogTemplates>() ??
            throw new InvalidOperationException(
                "ILogTemplates not found, " +
                "WithConnectionToMessageBroker has not been called.");

        internal void CompleteWithDefaults()
        {
            // if (!SilverbackBuilder.Services.ContainsAny<IInboundConnector>())
            //     this.AddInboundConnector();

            if (!SilverbackBuilder.Services.ContainsAny<IOutboundConnector>())
                this.AddOutboundConnector();

            if (!SilverbackBuilder.Services.ContainsAny<BrokerConnectionOptions>())
                this.WithConnectionOptions(new BrokerConnectionOptions());
        }
    }
}
