// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client.Publishing;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public class MqttProducer : Producer<MqttBroker, MqttProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public MqttProducer(
            MqttBroker broker,
            MqttProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<MqttProducer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            ClientWrapper = serviceProvider
                .GetRequiredService<IMqttClientsCache>()
                .GetClient(this);
        }

        internal MqttClientWrapper ClientWrapper { get; }

        /// <inheritdoc cref="Producer.ConnectCoreAsync" />
        protected override Task ConnectCoreAsync() =>
            ClientWrapper.ConnectAsync(this);

        /// <inheritdoc cref="Producer.DisconnectCoreAsync" />
        protected override Task DisconnectCoreAsync() =>
            ClientWrapper.DisconnectAsync(this);

        /// <inheritdoc cref="Producer.ProduceCore" />
        protected override IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() => ProduceCoreAsync(envelope));

        /// <inheritdoc cref="Producer.ProduceCoreAsync" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (!ClientWrapper.MqttClient.IsConnected)
                throw new InvalidOperationException("The client is not connected.");

            var mqttApplicationMessage = new MqttApplicationMessage
            {
                Topic = Endpoint.Name,
                Payload = await envelope.RawMessage.ReadAllAsync().ConfigureAwait(false),
                QualityOfServiceLevel = Endpoint.QualityOfServiceLevel,
                Retain = Endpoint.Retain,
                MessageExpiryInterval = Endpoint.MessageExpiryInterval,
            };

            if (Endpoint.Configuration.AreHeadersSupported)
                mqttApplicationMessage.UserProperties = envelope.Headers.ToUserProperties();

            var result = await ClientWrapper.MqttClient.PublishAsync(
                mqttApplicationMessage,
                CancellationToken.None).ConfigureAwait(false);

            if (result.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                throw new MqttProduceException(
                    "Error occurred producing the message to the MQTT broker. See the Result property for details.",
                    result);
            }

            return null;
        }
    }
}
