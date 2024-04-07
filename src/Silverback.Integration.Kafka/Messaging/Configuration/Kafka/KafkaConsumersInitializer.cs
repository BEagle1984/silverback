// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

// TODO: Unit test?
internal class KafkaConsumersInitializer : BrokerClientsInitializer
{
    private readonly KafkaClientsConfigurationActions _configurationActions;

    private readonly IBrokerClientCallbacksInvoker _brokerClientCallbacksInvoker;

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly ISilverbackLoggerFactory _silverbackLoggerFactory;

    private readonly IConsumerLogger<KafkaConsumer> _consumerLogger;

    private readonly IBrokerBehaviorsProvider<IConsumerBehavior> _behaviorsProvider;

    public KafkaConsumersInitializer(
        KafkaClientsConfigurationActions configurationActions,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        IKafkaOffsetStoreFactory offsetStoreFactory,
        ISilverbackLoggerFactory silverbackLoggerFactory,
        IConsumerLogger<KafkaConsumer> consumerLogger,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider,
        ISilverbackLogger<KafkaConsumersInitializer> logger)
        : base(serviceProvider, logger)
    {
        _configurationActions = Check.NotNull(configurationActions, nameof(configurationActions));
        _brokerClientCallbacksInvoker = Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _silverbackLoggerFactory = Check.NotNull(silverbackLoggerFactory, nameof(silverbackLoggerFactory));
        _consumerLogger = Check.NotNull(consumerLogger, nameof(consumerLogger));
        _behaviorsProvider = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifecycle handled by the ConsumerCollection")]
    protected override void InitializeCore()
    {
        foreach (MergedAction<KafkaConsumerConfigurationBuilder> mergedAction in _configurationActions.ConsumerConfigurationActions)
        {
            KafkaConsumerConfigurationBuilder builder = new();
            mergedAction.Action.Invoke(builder);
            KafkaConsumerConfiguration configuration = builder.Build();

            ConfluentConsumerWrapper confluentConsumerWrapper = new(
                mergedAction.Key,
                ServiceProvider.GetRequiredService<IConfluentConsumerBuilder>(),
                configuration,
                ServiceProvider.GetRequiredService<IConfluentAdminClientBuilder>(),
                _brokerClientCallbacksInvoker,
                _offsetStoreFactory,
                ServiceProvider,
                _silverbackLoggerFactory.CreateLogger<ConfluentConsumerWrapper>());

            AddClient(confluentConsumerWrapper);

            KafkaConsumer consumer = new(
                mergedAction.Key,
                confluentConsumerWrapper,
                configuration,
                _behaviorsProvider,
                _brokerClientCallbacksInvoker,
                _offsetStoreFactory,
                ServiceProvider,
                _consumerLogger);

            AddConsumer(consumer);
        }
    }
}
