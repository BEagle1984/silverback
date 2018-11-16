// TODO: How to support? Same approach as per the outbound routing? -> Subscribe method can have a return! (that is relayed)

//using System;
//using Silverback.Messaging.Messages;

//namespace Silverback.Messaging.Connectors
//{
//    /// <summary>
//    /// Translates an <see cref="IMessage"/> into another type of <see cref="IMessage"/>. Mostly used to convert
//    /// the internal <see cref="IMessage"/> to <see cref="IIntegrationMessage"/> to be sent over the broker.
//    /// </summary>
//    /// <seealso cref="MessageTranslator{TSource,TDestination}" />
//    public class GenericMessageTranslator<TSource, TDestination> : MessageTranslator<TSource, TDestination>
//        where TSource : IMessage
//        where TDestination : IMessage
//    {
//        private readonly Func<TSource, TDestination> _mapper;

//        public GenericMessageTranslator(Func<TSource, TDestination> mapper)
//        {
//            _mapper = mapper;
//        }

//        protected override TDestination Map(TSource source)
//            => _mapper(source);
//    }
//}