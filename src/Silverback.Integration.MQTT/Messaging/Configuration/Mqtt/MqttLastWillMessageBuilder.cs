// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <inheritdoc cref="IMqttLastWillMessageBuilder" />
    public class MqttLastWillMessageBuilder : IMqttLastWillMessageBuilder
    {
        private string? _topic;

        private object? _message;

        private MqttQualityOfServiceLevel _qosLevel;

        private IMessageSerializer? _serializer;

        private bool _retain;

        private uint? _delay;

        private string? _contentType;

        private byte[]? _correlationData;

        private string? _responseTopic;

        private MqttPayloadFormatIndicator _formatIndicator;

        private List<MqttUserProperty> _userProperties = new();

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

            _delay = (uint)delay.TotalSeconds;
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

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.Retain" />
        public IMqttLastWillMessageBuilder Retain()
        {
            _retain = true;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.SerializeUsing" />
        public IMqttLastWillMessageBuilder SerializeUsing(IMessageSerializer serializer)
        {
            _serializer = serializer;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.SerializeAsJson" />
        public IMqttLastWillMessageBuilder SerializeAsJson(Action<IJsonMessageSerializerBuilder>? serializerBuilderAction = null)
        {
            var serializerBuilder = new JsonMessageSerializerBuilder();
            serializerBuilderAction?.Invoke(serializerBuilder);
            return SerializeUsing(serializerBuilder.Build());
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithContentType" />
        public IMqttLastWillMessageBuilder WithContentType(string? contentType)
        {
            _contentType = contentType;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithCorrelationData" />
        public IMqttLastWillMessageBuilder WithCorrelationData(byte[]? correlationData)
        {
            _correlationData = correlationData;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithResponseTopic" />
        public IMqttLastWillMessageBuilder WithResponseTopic(string? topic)
        {
            _responseTopic = topic;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.WithPayloadFormatIndicator" />
        public IMqttLastWillMessageBuilder WithPayloadFormatIndicator(MqttPayloadFormatIndicator formatIndicator)
        {
            _formatIndicator = formatIndicator;
            return this;
        }

        /// <inheritdoc cref="IMqttLastWillMessageBuilder.AddUserProperty" />
        public IMqttLastWillMessageBuilder AddUserProperty(string name, string value)
        {
            _userProperties.Add(new MqttUserProperty(name, value));
            return this;
        }

        /// <summary>
        ///     Build the will message into the specified options builder.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="MqttClientOptionsBuilder"/>.
        /// </param>
        public void Build(MqttClientOptionsBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            if (string.IsNullOrEmpty(_topic))
                throw new EndpointConfigurationException("The topic name was not specified.");

            _serializer ??= new JsonMessageSerializer();
            var payloadStream = AsyncHelper.RunValueTaskSynchronously(
                () => _serializer.SerializeAsync(
                    _message,
                    new MessageHeaderCollection(),
                    MessageSerializationContext.Empty));

            if (_delay.HasValue)
                builder.WithWillDelayInterval(_delay.Value);

            builder.WithWillPayload(payloadStream.ReadAll());
            builder.WithWillQualityOfServiceLevel(_qosLevel);
            builder.WithWillTopic(_topic);
            builder.WithWillRetain(_retain);
            builder.WithWillContentType(_contentType);
            builder.WithWillCorrelationData(_correlationData);
            builder.WithWillResponseTopic(_responseTopic);
            builder.WithWillPayloadFormatIndicator(_formatIndicator);

            foreach (MqttUserProperty userProperty in _userProperties)
            {
                builder.WithWillUserProperty(userProperty.Name, userProperty.Value);
            }
        }
    }
}
