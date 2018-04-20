using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Repositories;

namespace Silverback.Messaging.Adapters
{

    /// <summary>
    /// Stores the <see cref="IMessage" /> into an outbox table to be fowarded to the message broker later on.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <seealso cref="Silverback.Messaging.Adapters.IOutboundAdapter" />
    public class DbOutboundAdapter<TEntity> : IOutboundAdapter
        where TEntity : IOutboundMessageEntity
    {
        private readonly IOutboundMessagesRepository<TEntity> _outboxRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Silverback.Integration.Adapters.DbOutboundAdapter`1" /> class.
        /// </summary>
        /// <param name="outboxRepository">The outbox repository.</param>
        public DbOutboundAdapter(IOutboundMessagesRepository<TEntity> outboxRepository)
        {
            _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        }
        
        /// <summary>
        /// Publishes the <see cref="T:Silverback.Messaging.Messages.IIntegrationMessage" /> to the specified <see cref="T:Silverback.Messaging.IEndpoint" />.
        /// </summary>
        /// <param name="message">The message to be relayed.</param>
        /// <param name="producer">The producer to be used to send the message.</param>
        /// <param name="endpoint">The endpoint.</param>
        public void Relay(IIntegrationMessage message, IProducer producer, IEndpoint endpoint)
        {
            var entity = _outboxRepository.Create();

            entity.Created = DateTime.UtcNow;

            entity.EndpointType = endpoint.GetType().AssemblyQualifiedName;
            entity.Endpoint = JsonConvert.SerializeObject(endpoint);

            entity.MessageType = message.GetType().AssemblyQualifiedName;
            entity.Message = JsonConvert.SerializeObject(message);

            entity.MessageId = Guid.NewGuid();

            _outboxRepository.Add(entity);
        }
    }
}