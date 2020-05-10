// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> AsObservable </c> method to the <see cref="ISilverbackBuilder"/>.
    /// </summary>
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        ///     Allows the subscribers to receive an <see cref="IObservable{T}" /> as parameter. It also
        ///     registers the <see cref="IMessageObservable{TMessage}" /> that can be used to process the
        ///     entire messages stream using Rx.NET.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AsObservable(this ISilverbackBuilder silverbackBuilder)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services
                .AddSingleton<IArgumentResolver, ObservableMessageArgumentResolver>()
                .AddScoped<IReturnValueHandler, ObservableMessagesReturnValueHandler>()
                .AddSingleton<MessageObservable, MessageObservable>()
                .AddSingleton(typeof(IMessageObservable<>), typeof(MessageObservable<>));

            silverbackBuilder
                .AddSingletonSubscriber<MessageObservable>();

            return silverbackBuilder;
        }
    }
}
