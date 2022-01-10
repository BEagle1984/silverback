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
// ///     Adds the AddInboundLog methods to the <see cref="BrokerOptionsBuilder" />.
// /// </content>
// public sealed partial class BrokerOptionsBuilder
// {
//     /// <summary>
//     ///     <para>
//     ///         Adds the necessary services to enable the <see cref="LogExactlyOnceStrategy" />.
//     ///     </para>
//     ///     <para>
//     ///         The <see cref="LogExactlyOnceStrategy" /> stores uses an <see cref="IInboundLog" /> to
//     ///         keep track of to keep track of each processed message and guarantee that each one is processed
//     ///         only once.
//     ///     </para>
//     /// </summary>
//     /// <typeparam name="TInboundLog">
//     ///     The type of the <see cref="IInboundLog" /> to be used.
//     /// </typeparam>
//     /// <returns>
//     ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
//     /// </returns>
//     public BrokerOptionsBuilder AddInboundLog<TInboundLog>()
//         where TInboundLog : class, IInboundLog
//     {
//         SilverbackBuilder.Services.AddScoped<IInboundLog, TInboundLog>();
//         return this;
//     }
//
//     /// <summary>
//     ///     <para>
//     ///         Adds the necessary services to enable the <see cref="LogExactlyOnceStrategy" /> storing the messages identifiers in memory.
//     ///     </para>
//     ///     <para>
//     ///         The <see cref="LogExactlyOnceStrategy" /> stores uses an <see cref="IInboundLog" /> to keep track of to keep track of each
//     ///         processed message and guarantee that each one is processed only once.
//     ///     </para>
//     /// </summary>
//     /// <returns>
//     ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
//     /// </returns>
//     public BrokerOptionsBuilder AddInMemoryInboundLog() => AddInboundLog<InMemoryInboundLog>();
//
//     /// <summary>
//     ///     <para>
//     ///         Adds the necessary services to enable the <see cref="LogExactlyOnceStrategy" /> using a
//     ///         database table as store.
//     ///     </para>
//     ///     <para>
//     ///         The <see cref="LogExactlyOnceStrategy" /> stores uses an <see cref="IInboundLog" /> to keep track of to keep track of each
//     ///         processed message and guarantee that each one is processed only once.
//     ///     </para>
//     /// </summary>
//     /// <returns>
//     ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
//     /// </returns>
//     public BrokerOptionsBuilder AddInboundLogDatabaseTable() => AddInboundLog<DbInboundLog>();
// }
