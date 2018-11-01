using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Repositories;

namespace Silverback.Messaging
{
    // TODO 2

    ///// <summary>
    ///// Publishes the messages in the outbox queue to the configured message broker.
    ///// </summary>
    //public class OutboundMessagesWorker<TEntity>
    //    where TEntity : class, IOutboundMessageEntity
    //{
    //    private readonly IOutboundMessagesRepository<TEntity> _repository;
    //    private readonly IBroker _broker;

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="OutboundMessagesWorker{TEntity}" /> class.
    //    /// </summary>
    //    /// <param name="repository">The repository.</param>
    //    /// <param name="broker">The broker.</param>
    //    public OutboundMessagesWorker(IOutboundMessagesRepository<TEntity> repository, IBroker broker)
    //    {
    //        _repository = repository;
    //        _broker = broker;
    //    }

    //    /// <summary>
    //    /// Sends the pending messages.
    //    /// </summary>
    //    public void SendPendingMessages()
    //    {
    //        var entities = _repository.GetPending();

    //        foreach (var entity in entities)
    //        {
    //            //try
    //            {
    //                var endpointType = ReflectionHelper.GetType(entity.EndpointType);
    //                var endpoint = (IEndpoint)JsonConvert.DeserializeObject(entity.Endpoint, endpointType);

    //                var messageType = ReflectionHelper.GetType(entity.MessageType);
    //                var message = (IIntegrationMessage)JsonConvert.DeserializeObject(entity.Message, messageType);

    //                SendMessage(message, endpoint);

    //                entity.Sent = DateTime.UtcNow;

    //                _repository.SaveChanges();
    //            }
    //            //catch
    //            {
    //                // TODO: Log and...?
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Sends the specified message to the message broker.
    //    /// </summary>
    //    /// <param name="message">The original message.</param>
    //    /// <param name="endpoint">The message broker endpoint.</param>
    //    protected virtual void SendMessage(IIntegrationMessage message, IEndpoint endpoint)
    //        => _broker.GetProducer(endpoint).Produce(Envelope.Create(message));
    //}
}
