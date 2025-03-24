// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <see cref="AsObservable" /> method to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderAsObservableExtensions
{
    /// <summary>
    ///     Allows the subscribers to receive an <see cref="IObservable{T}" /> or an <see cref="IMessageStreamObservable{TMessage}" />
    ///     as parameter.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AsObservable(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services
            .AddSingleton<IArgumentResolver, ObservableStreamMessageArgumentResolver>()
            .AddSingleton<IReturnValueHandler, ObservableMessagesReturnValueHandler>();

        return builder;
    }
}
