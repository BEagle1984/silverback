using System;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Repositories;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that subscribes to the message broker and forwards the messages to the internal bus.<br />
    /// This implementation uses an inbox table to prevent duplicated processing of the same message.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <seealso cref="Silverback.Messaging.Adapters.SimpleInboundAdapter" />
    /// <seealso cref="Silverback.Messaging.Adapters.IInboundAdapter" />
    public class DbInboundAdapter<TEntity> : SimpleInboundAdapter
        where TEntity : IInboundMessageEntity
    {
        private readonly IInboundMessagesRepository<TEntity> _inboxRepository;
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbInboundAdapter{TEntity}"/> class.
        /// </summary>
        /// <param name="inboxRepository">The inbox repository.</param>
        /// <exception cref="ArgumentNullException">inboxRepository</exception>
        public DbInboundAdapter(IInboundMessagesRepository<TEntity> inboxRepository)
        {
            _inboxRepository = inboxRepository ?? throw new ArgumentNullException(nameof(inboxRepository));
        }

        /// <summary>
        /// Initializes the <see cref="T:Silverback.Messaging.Adapters.IInboundAdapter" />.
        /// </summary>
        /// <param name="bus">The internal <see cref="T:Silverback.Messaging.IBus" /> where the messages have to be relayed.</param>
        /// <param name="endpoint">The endpoint this adapter has to connect to.</param>
        /// <param name="errorPolicy">An optional error handling policy.</param>
        public override void Init(IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            _logger = bus.GetLoggerFactory().CreateLogger<DbInboundAdapter<TEntity>>();
            base.Init(bus, endpoint, errorPolicy);
        }

        /// <summary>
        /// Relays the message ensuring that it wasn't processed already by this microservice.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void RelayMessage(IIntegrationMessage message)
        {
            // TODO: IMPORTANT: The check must be extended to Id + QUEUE in order to allow retry of messages that are moved to a retry queue
            if (_inboxRepository.Exists(message.Id))
            {
                _logger.LogInformation($"Message '{message.Id}' is being skipped since it was already processed.");
                return;
            }

            var entity = _inboxRepository.Create();
            entity.MessageId = message.Id;
            entity.Received = DateTime.UtcNow;
            _inboxRepository.Add(entity);

            base.RelayMessage(message);

            // Call save changes, in case the changes weren't committed
            // already as part of the message handling transaction.
            _inboxRepository.SaveChanges();
        }
    }
}