// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public class InboundConnector : IInboundConnector
    {
        private readonly IBroker _broker;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly List<InboundConsumer> _inboundConsumers = new List<InboundConsumer>();

        public InboundConnector(IBroker broker, IServiceProvider serviceProvider, ILogger<InboundConnector> logger)
        {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual IInboundConnector Bind(IEndpoint endpoint, IErrorPolicy errorPolicy = null, InboundConnectorSettings settings = null)
        {
            settings = settings ?? new InboundConnectorSettings();

            for (int i = 0; i < settings.Consumers; i++)
            {
                _inboundConsumers.Add(new InboundConsumer(
                    _broker,
                    endpoint,
                    settings, 
                    RelayMessages,
                    Commit,
                    Rollback,
                    errorPolicy, 
                    _serviceProvider));
            }

            // TODO: Carefully test with multiple endpoints!
            // TODO: Test if consumer gets properly disposed etc.
            return this;
        }

        protected virtual void RelayMessages(IEnumerable<MessageReceivedEventArgs> messagesArgs, IEndpoint endpoint, IServiceProvider serviceProvider)
        {
            var messages = messagesArgs
                .Select(args => HandleChunkedMessage(args, endpoint, serviceProvider) ? args.Message : null)
                .Select(msg =>
                    msg is FailedMessage failedMessage
                        ? failedMessage.Message
                        : msg)
                .Where(msg => msg != null)
                .ToList();

            if (!messages.Any())
                return;

            serviceProvider.GetRequiredService<IPublisher>().Publish(messages);
        }

        private bool HandleChunkedMessage(MessageReceivedEventArgs args, IEndpoint endpoint, IServiceProvider serviceProvider)
        {
            if (args.Message is MessageChunk chunk)
            {
                var joined = serviceProvider.GetRequiredService<ChunkConsumer>().JoinIfComplete(chunk);

                if (joined == null)
                    return false;

                args.Message = endpoint.Serializer.Deserialize(joined);
            }

            return true;
        }

        protected virtual void Commit(IServiceProvider serviceProvider)
        {
            var chunkConsumer = serviceProvider.GetService<ChunkConsumer>();
            chunkConsumer?.CleanupProcessedMessages();
            chunkConsumer?.Commit();
        }

        protected virtual void Rollback(IServiceProvider serviceProvider)
        {
            var chunkConsumer = serviceProvider.GetService<ChunkConsumer>();
            chunkConsumer?.Rollback();
        }
    }
}