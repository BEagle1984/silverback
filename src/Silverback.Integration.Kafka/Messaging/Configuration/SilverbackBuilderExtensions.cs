// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        ///     Registers Apache Kafka as message broker.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsAction">Additional options (such as connectors).</param>
        /// <returns></returns>
        [Obsolete("Use WithConnectionToMessageBroker and AddKafka instead.")]
        public static ISilverbackBuilder WithConnectionToKafka(
            this ISilverbackBuilder builder,
            Action<IBrokerOptionsBuilder> optionsAction = null) =>
            builder.WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                optionsAction?.Invoke(options);
            });
    }
}