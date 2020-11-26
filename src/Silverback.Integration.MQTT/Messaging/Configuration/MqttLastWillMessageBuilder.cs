// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet;
using MQTTnet.Protocol;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <inheritdoc cref="IMqttLastWillMessageBuilder" />
    public class MqttLastWillMessageBuilder : IMqttLastWillMessageBuilder
    {
        private string? _topic;

        private object? _message;

        private MqttQualityOfServiceLevel _qosLevel;

        private IMessageSerializer? _serializer;

        /// <summary>
        ///     Gets the desired delay in seconds.
        /// </summary>
        public uint? Delay { get; private set; }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.ProduceTo" />
        public IMqttLastWillMessageBuilder ProduceTo(string topicName)
        {
            _topic = Check.NotEmpty(topicName, nameof(topicName));
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.Message" />
        public IMqttLastWillMessageBuilder Message(object message)
        {
            _message = Check.NotNull(message, nameof(message));
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithDelay(TimeSpan)" />
        public IMqttLastWillMessageBuilder WithDelay(TimeSpan delay)
        {
            Check.Range(delay, nameof(delay), TimeSpan.Zero, TimeSpan.FromSeconds(uint.MaxValue));

            Delay = (uint)delay.TotalSeconds;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithQualityOfServiceLevel" />
        public IMqttLastWillMessageBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
        {
            _qosLevel = qosLevel;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithAtMostOnceQoS" />
        public IMqttLastWillMessageBuilder WithAtMostOnceQoS() =>
            WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce);

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithAtLeastOnceQoS" />
        public IMqttLastWillMessageBuilder WithAtLeastOnceQoS() =>
            WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithExactlyOnceQoS" />
        public IMqttLastWillMessageBuilder WithExactlyOnceQoS() =>
            WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.SerializeUsing" />
        public IMqttLastWillMessageBuilder SerializeUsing(IMessageSerializer serializer)
        {
            _serializer = serializer;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.SerializeAsJson" />
        public IMqttLastWillMessageBuilder SerializeAsJson(
            Action<IJsonMessageSerializerBuilder>? serializerBuilderAction = null)
        {
            var serializerBuilder = new JsonMessageSerializerBuilder();
            serializerBuilderAction?.Invoke(serializerBuilder);
            return SerializeUsing(serializerBuilder.Build());
        }

        /// <summary>
        ///     Builds the <see cref="MqttApplicationMessage" /> instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="MqttApplicationMessage" />.
        /// </returns>
        public MqttApplicationMessage Build()
        {
            if (string.IsNullOrEmpty(_topic))
                throw new EndpointConfigurationException("The topic name was not specified.");

            _serializer ??= new JsonMessageSerializer();
            var payloadStream = AsyncHelper.RunValueTaskSynchronously(
                () => _serializer.SerializeAsync(
                    _message,
                    new MessageHeaderCollection(),
                    MessageSerializationContext.Empty));

            return new MqttApplicationMessage
            {
                Topic = _topic,
                Payload = payloadStream.ReadAll(),
                QualityOfServiceLevel = _qosLevel
            };
        }
    }
}
