// TODO: DELETE

//using System;
//using System.Threading.Tasks;
//using Silverback.Util;
//using Silverback.Messaging.Messages;
//using Microsoft.Extensions.Logging;

//namespace Silverback.Messaging.Subscribers
//{
//    public abstract class AsyncSubscriber<TMessage> : SubscriberBase<TMessage>
//        where TMessage : IMessage
//    {
//        private readonly ILogger<AsyncSubscriber<TMessage>> _logger;

//        protected AsyncSubscriber(ILogger<AsyncSubscriber<TMessage>> logger) : base(logger)
//        {
//            _logger = logger;
//        }

//        [Subscribe]
//        public Task OnMessageReceived(TMessage message)
//        {
//            _logger.LogTrace($"Asynchronously processing message of type '{typeof(TMessage).Name}'.");

//            return MustHandle(message) ? HandleAsync(message) : Task.CompletedTask;
//        }

//        public abstract Task HandleAsync(TMessage message);
//    }
//}