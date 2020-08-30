// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddOffsetStore</c> and related methods to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddOffsetStoreExtensions
    {
        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="OffsetStoreExactlyOnceStrategy" />.
        ///     </para>
        ///     <para>
        ///         The <see cref="OffsetStoreExactlyOnceStrategy" /> stores uses an <see cref="IOffsetStore" /> to
        ///         keep track of the latest processed offsets and guarantee that each message is processed only once.
        ///     </para>
        /// </summary>
        /// <typeparam name="TOffsetStore">
        ///     The type of the <see cref="IOffsetStore" /> to be used.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOffsetStore<TOffsetStore>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TOffsetStore : class, IOffsetStore
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services
                .AddScoped<IOffsetStore, TOffsetStore>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="OffsetStoreExactlyOnceStrategy" /> storing the
        ///         offsets in memory.
        ///     </para>
        ///     <para>
        ///         The <see cref="OffsetStoreExactlyOnceStrategy" /> stores uses an <see cref="IOffsetStore" /> to
        ///         keep track of the latest processed offsets and guarantee that each message is processed only once.
        ///     </para>
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddInMemoryOffsetStore(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddOffsetStore<InMemoryOffsetStore>();
        }

        /// <summary>
        ///     <para>
        ///         Adds the necessary services to enable the <see cref="OffsetStoreExactlyOnceStrategy" /> using a
        ///         database table as store.
        ///     </para>
        ///     <para>
        ///         The <see cref="OffsetStoreExactlyOnceStrategy" /> stores uses an <see cref="IOffsetStore" /> to
        ///         keep track of the latest processed offsets and guarantee that each message is processed only once.
        ///     </para>
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOffsetStoreDatabaseTable(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddOffsetStore<DbOffsetStore>();
        }
    }
}
