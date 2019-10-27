// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public class InboundConnector : IInboundConnector
    {
        private readonly IBroker _broker;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<InboundConsumer> _inboundConsumers = new List<InboundConsumer>();

        public InboundConnector(IBroker broker, IServiceProvider serviceProvider)
        {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public virtual IInboundConnector Bind(IEndpoint endpoint, IErrorPolicy errorPolicy = null, InboundConnectorSettings settings = null)
        {
            settings ??= new InboundConnectorSettings();

            for (var i = 0; i < settings.Consumers; i++)
            {
                _inboundConsumers.Add(new InboundConsumer(
                    _broker,
                    endpoint,
                    settings,
                    HandleMessages,
                    Commit,
                    Rollback,
                    errorPolicy,
                    _serviceProvider));
            }

            // TODO: Carefully test with multiple endpoints!
            // TODO: Test if consumer gets properly disposed etc.
            return this;
        }

        protected async Task HandleMessages(IEnumerable<IInboundMessage> messages, IServiceProvider serviceProvider)
        {
            var deserializedMessages = await messages
                .SelectAsync(async message => await HandleChunkedMessage(message, serviceProvider));
                
            deserializedMessages = deserializedMessages 
                .Where(args => args != null)
                .Select(DeserializeRawMessage)
                .ToList();

            if (!deserializedMessages.Any())
                return;

            await RelayMessages(deserializedMessages, serviceProvider);
        }

        private async Task<IInboundMessage> HandleChunkedMessage(IInboundMessage message, IServiceProvider serviceProvider)
        {
            if (!message.Headers.Contains(MessageHeader.ChunkIdKey))
                return message;

            var completeMessage = await serviceProvider.GetRequiredService<ChunkConsumer>().JoinIfComplete(message);

            return completeMessage == null 
                ? null 
                : new InboundMessage(completeMessage, message.Headers, message.Offset, message.Endpoint, message.MustUnwrap);
        }

        private IInboundMessage DeserializeRawMessage(IInboundMessage message)
        {
            var deserialized =
                message.Content ?? (((InboundMessage) message).Content =
                    message.Endpoint.Serializer.Deserialize(message.RawContent, message.Headers));

            // Create typed message for easier specific subscription
            var typedInboundMessage = (InboundMessage) Activator.CreateInstance(
                typeof(InboundMessage<>).MakeGenericType(deserialized.GetType()),
                message);

            typedInboundMessage.Content = deserialized;

            return typedInboundMessage;
        }

        protected virtual async Task RelayMessages(IEnumerable<IInboundMessage> messages, IServiceProvider serviceProvider) => 
            await serviceProvider.GetRequiredService<IPublisher>().PublishAsync(messages);

        protected virtual async Task Commit(IServiceProvider serviceProvider)
        {
            var chunkConsumer = serviceProvider.GetService<ChunkConsumer>();
            if (chunkConsumer != null)
                await chunkConsumer.Commit();
        }

        protected virtual async Task Rollback(IServiceProvider serviceProvider)
        {
            var chunkConsumer = serviceProvider.GetService<ChunkConsumer>();
            if (chunkConsumer != null)
                await chunkConsumer.Rollback();
        }
    }
}