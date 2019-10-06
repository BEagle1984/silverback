// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        /// Registers the message broker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message broker implementation.</typeparam>
        /// <param name="builder"></param>
        /// <param name="optionsAction">Additional options (such as connectors).</param>
        /// <returns></returns>
        public static ISilverbackBuilder WithConnectionTo<T>(this ISilverbackBuilder builder, Action<BrokerOptionsBuilder> optionsAction = null)
            where T : class, IBroker
        {
            builder.Services
                .AddSingleton<IBroker, T>()
                .AddSingleton<ErrorPolicyBuilder>()
                .AddSingleton<IMessageKeyProvider, DefaultPropertiesMessageKeyProvider>()
                .AddSingleton<MessageKeyProvider>()
                .AddSingleton<MessageLogger>();

            var options = new BrokerOptionsBuilder(builder);
            optionsAction?.Invoke(options);
            options.CompleteWithDefaults();

            return builder;
        }
    }
}
