// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the obsolete <c> AddMessageIdProvider </c> to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddMessageIdProviderExtensions
    {
#pragma warning disable 618

        /// <summary>
        ///     Adds a message id provider of type <typeparamref name="TProvider" /> to be used to auto-generate the
        ///     unique id of the integration messages.
        /// </summary>
        /// <typeparam name="TProvider">
        ///     The type of the <see cref="IMessageIdProvider" /> to add.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        [Obsolete("This feature will be removed in a future release. Please use a behavior instead.")]
        public static IBrokerOptionsBuilder AddMessageIdProvider<TProvider>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TProvider : class, IMessageIdProvider
        {
            if (brokerOptionsBuilder == null)
                throw new ArgumentNullException(nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddSingleton<IMessageIdProvider, TProvider>();

            return brokerOptionsBuilder;
        }

#pragma warning restore 618
    }
}
