// TODO: DELETE
//using System;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Silverback.Messaging.Messages;

//namespace Silverback.Messaging.Subscribers
//{
//    public abstract class SubscriberBase<TMessage> : ISubscriber
//        where TMessage : IMessage
//    {
//        private readonly ILogger<SubscriberBase<TMessage>> _logger;

//        protected SubscriberBase(ILogger<SubscriberBase<TMessage>> logger)
//        {
//            _logger = logger;
//        }

//        public Func<TMessage, bool> Filter { get; set; }

//        protected bool MustHandle(TMessage message)
//        {
//            if (Filter == null || Filter.Invoke(message))
//                return true;

//            _logger.LogTrace("Discarding message because it was filtered out by the custom filter function.");
//            return false;
//        }
//    }
//}