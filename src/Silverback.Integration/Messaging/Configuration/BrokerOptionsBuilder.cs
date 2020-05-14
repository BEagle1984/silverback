// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
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

        internal void CompleteWithDefaults()
        {
            if (!SilverbackBuilder.Services.ContainsAny<IInboundConnector>())
                this.AddInboundConnector();

            if (!SilverbackBuilder.Services.ContainsAny<IOutboundConnector>())
                this.AddOutboundConnector();
        }
    }
}
