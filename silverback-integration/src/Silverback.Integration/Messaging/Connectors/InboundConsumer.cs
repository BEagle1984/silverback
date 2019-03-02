// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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

        private readonly Action<IEnumerable<MessageReceivedEventArgs>, IEndpoint, IServiceProvider> _messagesHandler;
        private readonly Action<IServiceProvider> _commitHandler;
        private readonly Action<IServiceProvider> _rollbackHandler;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        private readonly IConsumer _consumer;

        public InboundConsumer(IBroker broker,
            IEndpoint endpoint,
            InboundConnectorSettings settings,
            Action<IEnumerable<MessageReceivedEventArgs>, IEndpoint, IServiceProvider> messagesHandler,
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

                _consumer.Received += (_, args) => batch.AddMessage(args);
            }
            else
            {
                _consumer.Received += (_, args) => ProcessSingleMessage(args);
            }
        }

        private void ProcessSingleMessage(MessageReceivedEventArgs messageArgs)
        {
            _messageLogger.LogTrace(_logger, "Processing message.", messageArgs.Message, _endpoint, offset: messageArgs.Offset);

            _errorPolicy.TryProcess(messageArgs.Message, _ =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    RelayAndCommitSingleMessage(messageArgs, scope.ServiceProvider);
                }
            });
        }

        private void RelayAndCommitSingleMessage(MessageReceivedEventArgs messageArgs, IServiceProvider serviceProvider)
        {
            try
            {
                _messagesHandler(new[] {messageArgs}, _endpoint, serviceProvider);
                Commit(new[] {messageArgs.Offset}, serviceProvider);
            }
            catch (Exception ex)
            {
                _messageLogger.LogWarning(_logger, ex, "Error occurred processing the message.", messageArgs.Message, _endpoint, offset: messageArgs.Offset);
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