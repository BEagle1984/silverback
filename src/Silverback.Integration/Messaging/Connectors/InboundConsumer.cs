// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{

    // TODO: Test? (or implicitly tested with InboundConnector?)
    public class InboundConsumer
    {
        private readonly IEndpoint _endpoint;
        private readonly InboundConnectorSettings _settings;
        private readonly IErrorPolicy _errorPolicy;

        private readonly Action<IEnumerable<IInboundMessage>, IServiceProvider> _messagesHandler;
        private readonly Action<IServiceProvider> _commitHandler;
        private readonly Action<IServiceProvider> _rollbackHandler;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        private readonly IConsumer _consumer;

        public InboundConsumer(IBroker broker,
            IEndpoint endpoint,
            InboundConnectorSettings settings,
            Action<IEnumerable<IInboundMessage>, IServiceProvider> messagesHandler,
            Action<IServiceProvider> commitHandler,
            Action<IServiceProvider> rollbackHandler,
            IErrorPolicy errorPolicy,
            IServiceProvider serviceProvider)
        {
            _endpoint = endpoint;
            _settings = settings;
            _errorPolicy = errorPolicy;

            _messagesHandler = messagesHandler;
            _commitHandler = commitHandler;
            _rollbackHandler = rollbackHandler;

            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<InboundConsumer>>();
            _messageLogger = serviceProvider.GetRequiredService<MessageLogger>();

            _consumer = broker.GetConsumer(_endpoint);

            Bind();
        }

        private void Bind()
        {
            _logger.LogTrace($"Connecting to inbound endpoint '{_endpoint.Name}'...");

            _settings.Validate();

            if (_settings.Batch.Size > 1)
            {
                var batch = new MessageBatch(
                    _endpoint,
                    _settings.Batch,
                    _messagesHandler,
                    Commit,
                    _rollbackHandler,
                    _errorPolicy,
                    _serviceProvider);

                _consumer.Received += (_, args) => batch.AddMessage(CreateInboundMessage(args));
            }
            else
            {
                _consumer.Received += (_, args) => ProcessSingleMessage(CreateInboundMessage(args));
            }
        }

        private IInboundMessage CreateInboundMessage(MessageReceivedEventArgs args)
        {
            // TODO: Policies!
            var deserializedMessage = _endpoint.Serializer.Deserialize(args.Message);

            var message = (InboundMessage) Activator.CreateInstance(typeof(InboundMessage<>).MakeGenericType(deserializedMessage.GetType()));
            message.Message = deserializedMessage;
            message.Endpoint = _endpoint;
            message.MustUnwrap = _settings.UnwrapMessages;

            if (args.Headers != null)
                message.Headers.AddRange(args.Headers);

            return message;
        }

        private void ProcessSingleMessage(IInboundMessage message)
        {
            message.TryDeserializeAndProcess(
                _errorPolicy,
                deserializedMessage =>
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        RelayAndCommitSingleMessage(deserializedMessage, scope.ServiceProvider);
                    }
                });
        }

        private void RelayAndCommitSingleMessage(IInboundMessage message, IServiceProvider serviceProvider)
        {
            try
            {
                _messageLogger.LogInformation(_logger, "Processing message.", message);

                _messagesHandler(new[] { message }, serviceProvider);
                Commit(new[] { message.Offset }, serviceProvider);
            }
            catch (Exception ex)
            {
                _messageLogger.LogWarning(_logger, ex, "Error occurred processing the message.", message);
                Rollback(serviceProvider);
                throw;
            }
        }

        private void Commit(IEnumerable<IOffset> offsets, IServiceProvider serviceProvider)
        {
            _commitHandler?.Invoke(serviceProvider);
            _consumer.Acknowledge(offsets);
        }

        private void Rollback(IServiceProvider serviceProvider)
        {
            _rollbackHandler?.Invoke(serviceProvider);
        }
    }
}