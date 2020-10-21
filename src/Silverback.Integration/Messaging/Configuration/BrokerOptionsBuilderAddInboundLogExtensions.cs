// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddInboundLog</c> and related methods to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddInboundLogExtensions
    {
        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="LogExactlyOnceStrategy" />.
        ///     </para>
        ///     <para>
        ///         The <see cref="LogExactlyOnceStrategy" /> stores uses an <see cref="IInboundLog" /> to
        ///         keep track of to keep track of each processed message and guarantee that each one is processed
        ///         only once.
        ///     </para>
        /// </summary>
        /// <typeparam name="TInboundLog">
        ///     The type of the <see cref="IInboundLog" /> to be used.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddInboundLog<TInboundLog>(this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TInboundLog : class, IInboundLog
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services
                .AddScoped<IInboundLog, TInboundLog>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="LogExactlyOnceStrategy" /> storing the
        ///         offsets in memory.
        ///     </para>
        ///     <para>
        ///         The <see cref="LogExactlyOnceStrategy" /> stores uses an <see cref="IInboundLog" /> to
        ///         keep track of to keep track of each processed message and guarantee that each one is processed
        ///         only once.
        ///     </para>
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddInMemoryInboundLog(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddInboundLog<InMemoryInboundLog>();
        }

        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="LogExactlyOnceStrategy" /> using a
        ///         database table as store.
        ///     </para>
        ///     <para>
        ///         The <see cref="LogExactlyOnceStrategy" /> stores uses an <see cref="IInboundLog" /> to
        ///         keep track of to keep track of each processed message and guarantee that each one is processed
        ///         only once.
        ///     </para>
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddInboundLogDatabaseTable(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddInboundLog<DbInboundLog>();
        }
    }
}
