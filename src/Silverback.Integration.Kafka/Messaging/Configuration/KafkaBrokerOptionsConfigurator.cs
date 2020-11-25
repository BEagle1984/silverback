// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.ConfluentWrappers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     This class will be located via assembly scanning and invoked when a <see cref="KafkaBroker" /> is
    ///     added to the <see cref="IServiceCollection" />.
    /// </summary>
    public class KafkaBrokerOptionsConfigurator : IBrokerOptionsConfigurator<KafkaBroker>
    {
        /// <inheritdoc cref="IBrokerOptionsConfigurator{TBroker}.Configure" />
        public void Configure(IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder
                .AddSingletonBrokerBehavior<KafkaMessageKeyInitializerProducerBehavior>()
                .Services
                .AddTransient<IConfluentProducerBuilder, ConfluentProducerBuilder>()
                .AddTransient<IConfluentConsumerBuilder, ConfluentConsumerBuilder>();

            brokerOptionsBuilder.LogTemplates
                .ConfigureAdditionalData<KafkaConsumerEndpoint>("offset", "kafkaKey")
                .ConfigureAdditionalData<KafkaProducerEndpoint>("offset", "kafkaKey");
        }
    }
}
