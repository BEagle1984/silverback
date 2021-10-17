// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal sealed class BrokerOptionsBuilder : IBrokerOptionsBuilder
    {
        public BrokerOptionsBuilder(ISilverbackBuilder silverbackBuilder)
        {
            SilverbackBuilder = silverbackBuilder;
        }

        public ISilverbackBuilder SilverbackBuilder { get; }

        internal void CompleteWithDefaults()
        {
            if (!SilverbackBuilder.Services.ContainsAny<IOutboxReader>())
                SilverbackBuilder.Services.AddScoped<IOutboxReader, NullOutbox>();

            if (!SilverbackBuilder.Services.ContainsAny<BrokerConnectionOptions>())
                this.WithConnectionOptions(new BrokerConnectionOptions());
        }
    }
}
