// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public class InboundConnector : IInboundConnector
    {
        private readonly IBrokerCollection _brokerCollection;
        private readonly IServiceProvider _serviceProvider;

        [SuppressMessage("ReSharper", "CollectionNeverQueried.Local")]
        private readonly List<InboundConsumer> _inboundConsumers = new List<InboundConsumer>();

        public InboundConnector(IBrokerCollection brokerCollection, IServiceProvider serviceProvider)
        {
            _brokerCollection = brokerCollection ?? throw new ArgumentNullException(nameof(brokerCollection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public virtual IInboundConnector Bind(
            IConsumerEndpoint endpoint,
            IErrorPolicy errorPolicy = null,
            InboundConnectorSettings settings = null)
        {
            settings ??= new InboundConnectorSettings();

            for (var i = 0; i < settings.Consumers; i++)
            {
                _inboundConsumers.Add(new InboundConsumer(
                    _brokerCollection,
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

        protected async Task HandleMessages(IEnumerable<IInboundEnvelope> envelopes, IServiceProvider serviceProvider)
        {
            envelopes = (await envelopes
                    .SelectAsync(async envelope => await HandleChunkedMessage(envelope, serviceProvider)))
                .Where(args => args != null)
                .Select(DeserializeRawMessage)
                .ToList();

            if (!envelopes.Any())
                return;

            await RelayMessages(envelopes, serviceProvider);
        }

        private async Task<IInboundEnvelope> HandleChunkedMessage(
            IInboundEnvelope envelope,
            IServiceProvider serviceProvider)
        {
            if (!envelope.Headers.Contains(DefaultMessageHeaders.ChunkId))
                return envelope;

            var completeMessage = await serviceProvider.GetRequiredService<ChunkConsumer>().JoinIfComplete(envelope);

            return completeMessage == null
                ? null
                : new InboundEnvelope(
                    completeMessage,
                    envelope.Headers,
                    envelope.Offset,
                    envelope.Endpoint,
                    envelope.ActualEndpointName);
        }

        private IInboundEnvelope DeserializeRawMessage(IInboundEnvelope envelope)
        {
            var deserialized =
                envelope.Message ?? (((InboundEnvelope) envelope).Message =
                    envelope.Endpoint.Serializer.Deserialize(
                        envelope.RawMessage,
                        envelope.Headers,
                        new MessageSerializationContext(envelope.Endpoint, envelope.ActualEndpointName)));

            if (deserialized == null)
                return envelope;

            // Create typed message for easier specific subscription
            var typedInboundMessage = (InboundEnvelope) Activator.CreateInstance(
                typeof(InboundEnvelope<>).MakeGenericType(deserialized.GetType()),
                envelope);

            typedInboundMessage.Message = deserialized;

            return typedInboundMessage;
        }

        protected virtual async Task RelayMessages(
            IEnumerable<IInboundEnvelope> envelopes,
            IServiceProvider serviceProvider) =>
            await serviceProvider.GetRequiredService<IPublisher>().PublishAsync(envelopes);

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