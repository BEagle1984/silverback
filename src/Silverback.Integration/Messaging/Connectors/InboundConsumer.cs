// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IConsumerEndpoint _endpoint;
        private readonly InboundConnectorSettings _settings;
        private readonly IErrorPolicy _errorPolicy;
        private readonly ErrorPolicyHelper _errorPolicyHelper;

        private readonly Func<IEnumerable<IInboundMessage>, IServiceProvider, Task> _messagesHandler;
        private readonly Func<IServiceProvider, Task> _commitHandler;
        private readonly Func<IServiceProvider, Task> _rollbackHandler;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        private readonly IConsumer _consumer;

        public InboundConsumer(IBroker broker,
            IConsumerEndpoint endpoint,
            InboundConnectorSettings settings,
            Func<IEnumerable<IInboundMessage>, IServiceProvider, Task> messagesHandler,
            Func<IServiceProvider, Task> commitHandler,
            Func<IServiceProvider, Task> rollbackHandler,
            IErrorPolicy errorPolicy,
            IServiceProvider serviceProvider)
        {
            if (broker == null) throw new ArgumentNullException(nameof(broker));

            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _errorPolicy = errorPolicy;

            _messagesHandler = messagesHandler ?? throw new ArgumentNullException(nameof(messagesHandler));
            _commitHandler = commitHandler ?? throw new ArgumentNullException(nameof(commitHandler));
            _rollbackHandler = rollbackHandler ?? throw new ArgumentNullException(nameof(rollbackHandler));

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = serviceProvider.GetRequiredService<ILogger<InboundConsumer>>();
            _errorPolicyHelper = serviceProvider.GetRequiredService<ErrorPolicyHelper>();

            _consumer = broker.GetConsumer(_endpoint);

            Bind();
        }

        private void Bind()
        {
            _logger.LogDebug($"Connecting to inbound endpoint '{_endpoint.Name}'...");

            _settings.Validate();

            if (_settings.Batch.Size > 1)
            {
                var batch = new MessageBatch(
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

        private IInboundMessage CreateInboundMessage(MessageReceivedEventArgs args) =>
            new InboundMessage(args.Message, _settings.UnwrapMessages);

        private async Task ProcessSingleMessage(IInboundMessage message) =>
            await _errorPolicyHelper.TryProcessAsync(
                new[] { message },
                _errorPolicy,
                async messages =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    await RelayAndCommitSingleMessage(messages, scope.ServiceProvider);
                });

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private async Task RelayAndCommitSingleMessage(IEnumerable<IInboundMessage> messages,
            IServiceProvider serviceProvider)
        {
            IEnumerable<IOffset> offsets = null;
            try
            {
                offsets = messages.Select(m => m.Offset).ToList();

                await _messagesHandler(messages, serviceProvider);
                await Commit(offsets, serviceProvider);
            }
            catch (Exception)
            {
                await Rollback(offsets, serviceProvider);
                throw;
            }
        }

        private async Task Commit(IEnumerable<IOffset> offsets, IServiceProvider serviceProvider)
        {
            await _commitHandler.Invoke(serviceProvider);
            await _consumer.Commit(offsets);
        }

        private async Task Rollback(IEnumerable<IOffset> offsets, IServiceProvider serviceProvider)
        {
            if (offsets != null)
                await _consumer.Rollback(offsets);
            
            await _rollbackHandler.Invoke(serviceProvider);
        }
    }
}