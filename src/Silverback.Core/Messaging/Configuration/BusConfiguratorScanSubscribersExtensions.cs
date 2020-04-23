// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c> ScanSubscribers </c> method to the <see cref="IBusConfigurator" />.
    /// </summary>
    public static class BusConfiguratorScanSubscribersExtensions
    {
        /// <summary>
        ///     Resolves all the subscribers and build the types cache to speed-up the first
        ///     publish.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to be configured.
        /// </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator ScanSubscribers(this IBusConfigurator busConfigurator)
        {
            if (busConfigurator == null)
                throw new ArgumentNullException(nameof(busConfigurator));

            using var scope = busConfigurator.ServiceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<SubscribedMethodsLoader>().GetSubscribedMethods();
            return busConfigurator;
        }
    }
}
