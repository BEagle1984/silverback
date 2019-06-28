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
        private readonly ErrorPolicyHelper _errorPolicyHelper;

        private readonly Action<IEnumerable<IRawInboundMessage>, IServiceProvider> _messagesHandler;
        private readonly Action<IServiceProvider> _commitHandler;
        private readonly Action<IServiceProvider> _rollbackHandler;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        private readonly IConsumer _consumer;

        public InboundConsumer(IBroker broker,
            IEndpoint endpoint,
            InboundConnectorSettings settings,
            Action<IEnumerable<IRawInboundMessage>, IServiceProvider> messagesHandler,
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
            _errorPolicyHelper = serviceProvider.GetRequiredService<ErrorPolicyHelper>();

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

        private IRawInboundMessage CreateInboundMessage(MessageReceivedEventArgs args) =>
            new RawInboundMessage(
                args.Message,
                args.Headers,
                args.Offset,
                args.Endpoint,
                _settings.UnwrapMessages
            );

        private void ProcessSingleMessage(IRawInboundMessage message)
        {
            _errorPolicyHelper.TryProcess(
                new []{ message },
                _errorPolicy,
                messages =>
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        RelayAndCommitSingleMessage(messages, scope.ServiceProvider);
                    }
                });
        }

        private void RelayAndCommitSingleMessage(IEnumerable<IRawInboundMessage> messages, IServiceProvider serviceProvider)
        {
            try
            {
                _messagesHandler(messages, serviceProvider);
                Commit(messages.Select(m => m.Offset), serviceProvider);
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