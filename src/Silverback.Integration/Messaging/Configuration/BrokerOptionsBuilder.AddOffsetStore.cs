// TODO: DELETE
// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using Microsoft.Extensions.DependencyInjection;
// using Silverback.Messaging.Inbound.ExactlyOnce;
// using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
//
// namespace Silverback.Messaging.Configuration;
//
// /// <content>
// ///     Adds the AddOffsetStore methods to the <see cref="BrokerOptionsBuilder" />.
// /// </content>
// public sealed partial class BrokerOptionsBuilder
// {
//     /// <summary>
//     ///     <para>
//     ///         Adds the necessary services to enable the <see cref="OffsetStoreExactlyOnceStrategy" />.
//     ///     </para>
//     ///     <para>
//     ///         The <see cref="OffsetStoreExactlyOnceStrategy" /> stores uses an <see cref="IOffsetStore" /> to
//     ///         keep track of the latest processed offsets and guarantee that each message is processed only once.
//     ///     </para>
//     /// </summary>
//     /// <typeparam name="TOffsetStore">
//     ///     The type of the <see cref="IOffsetStore" /> to be used.
//     /// </typeparam>
//     /// <returns>
//     ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
//     /// </returns>
//     public BrokerOptionsBuilder AddOffsetStore<TOffsetStore>()
//         where TOffsetStore : class, IOffsetStore
//     {
//         SilverbackBuilder.Services.AddScoped<IOffsetStore, TOffsetStore>();
//         return this;
//     }
//
//     /// <summary>
//     ///     <para>
//     ///         Adds the necessary services to enable the <see cref="OffsetStoreExactlyOnceStrategy" /> storing
//     ///         the offsets in memory.
//     ///     </para>
//     ///     <para>
//     ///         The <see cref="OffsetStoreExactlyOnceStrategy" /> stores uses an <see cref="IOffsetStore" /> to
//     ///         keep track of the latest processed offsets and guarantee that each message is processed only once.
//     ///     </para>
//     /// </summary>
//     /// <returns>
//     ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
//     /// </returns>
//     public BrokerOptionsBuilder AddInMemoryOffsetStore() => AddOffsetStore<InMemoryOffsetStore>();
//
//     /// <summary>
//     ///     <para>
//     ///         Adds the necessary services to enable the <see cref="OffsetStoreExactlyOnceStrategy" /> using a
//     ///         database table as store.
//     ///     </para>
//     ///     <para>
//     ///         The <see cref="OffsetStoreExactlyOnceStrategy" /> stores uses an <see cref="IOffsetStore" /> to
//     ///         keep track of the latest processed offsets and guarantee that each message is processed only once.
//     ///     </para>
//     /// </summary>
//     /// <returns>
//     ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
//     /// </returns>
//     public BrokerOptionsBuilder AddOffsetStoreDatabaseTable() => AddOffsetStore<DbOffsetStore>();
// }
