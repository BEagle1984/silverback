// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>WithConnectionOptions</c> method to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderWithConnectionOptionsExtensions
    {
        /// <summary>
        ///     Specifies the <see cref="BrokerConnectionOptions" />.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="connectionOptions">
        ///     The <see cref="BrokerConnectionOptions" /> to apply.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder WithConnectionOptions(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            BrokerConnectionOptions connectionOptions)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddSingleton(connectionOptions);

            return brokerOptionsBuilder;
        }
    }
}
