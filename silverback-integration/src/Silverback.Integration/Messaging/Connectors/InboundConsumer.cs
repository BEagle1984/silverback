// Copyright (c) 2018 Sergio Aquilini
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

        private readonly Action<IMessage, IEndpoint, IServiceProvider> _messageHandler;
        private readonly Action<IServiceProvider> _commitHandler;
        private readonly Action<IServiceProvider> _rollbackHandler;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        private readonly IConsumer _consumer;

        public InboundConsumer(IBroker broker,
            IEndpoint endpoint,
            InboundConnectorSettings settings,
            Action<IMessage, IEndpoint, IServiceProvider> messageHandler,
            Action<IServiceProvider> commitHandler,
            Action<IServiceProvider> rollbackHandler,
            IErrorPolicy errorPolicy,
            IServiceProvider serviceProvider)
        {
            _endpoint = endpoint;
            _settings = settings;
            _errorPolicy = errorPolicy;

            _messageHandler = messageHandler;
            _commitHandler = commitHandler;
            _rollbackHandler = rollbackHandler;

            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<InboundConsumer>>();

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
                    _messageHandler,
                    Commit,
                    _rollbackHandler,
                    _errorPolicy,
                    _serviceProvider);

                _consumer.Received += (_, args) => batch.AddMessage(args.Message, args.Offset);
            }
            else
            {
                _consumer.Received += (_, args) => OnSingleMessageReceived(args.Message, args.Offset);
            }
        }

        private void OnSingleMessageReceived(IMessage message, IOffset offset)
        {
            _logger.LogTrace("Processing message.", message, _endpoint);

            _errorPolicy.TryProcess(message, _ =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    RelayAndCommitSingleMessage(message, offset, scope.ServiceProvider);
                }
            });
        }

        private void RelayAndCommitSingleMessage(IMessage message, IOffset offset, IServiceProvider serviceProvider)
        {
            try
            {
                _messageHandler(message, _endpoint, serviceProvider);
                Commit(new[] {offset}, serviceProvider);
            }
            catch (Exception)
            {
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