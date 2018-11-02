using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    public class SubscriberFactory<TSubscriber> : AsyncSubscriber<IMessage>
    {
        private readonly ITypeFactory _typeFactory;
        private ILogger _logger;

        public SubscriberFactory(ITypeFactory typeFactory)
        {
            _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
        }

        public override void Init(IBus bus)
        {
            base.Init(bus);
            _logger = bus.GetLoggerFactory().CreateLogger<SubscriberFactory<TSubscriber>>();
        }

        public override Task HandleAsync(IMessage message) =>
            GetSubscriberInstances()
                .SelectMany(GetSubscriberMethods)
                .Where(method => method.SubscribedMessageType.IsInstanceOfType(message))
                .ForEachAsync(method => ForwardMessage(method, message));
        
        private TSubscriber[] GetSubscriberInstances()
        {
            var subscribers = _typeFactory.GetInstances<TSubscriber>() ?? Array.Empty<TSubscriber>();

            _logger.LogTrace($"Resolved {subscribers.Length} object(s) of type '{typeof(TSubscriber).Name}'.");

            return subscribers;
        }

        private static SubscriberMethod[] GetSubscriberMethods(TSubscriber subscriber) => subscriber
            .GetType()
            .GetAnnotatedMethods<SubscribeAttribute>()
            .Select(methodInfo => GetSubscriberMethod(subscriber, methodInfo))
            .ToArray();

        private static SubscriberMethod GetSubscriberMethod(TSubscriber subscriber, MethodInfo methodInfo) =>
            new SubscriberMethod
            {
                Instance = subscriber,
                MethodInfo = methodInfo,
                SubscribedMessageType = GetMessageType(methodInfo)
            };

        private static Type GetMessageType(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();

            var messageType = parameters.FirstOrDefault()?.ParameterType;

            if (parameters.Length != 1 || !typeof(IMessage).IsAssignableFrom(messageType))
            {
                throw new SilverbackException(
                    $"The method {methodInfo.DeclaringType.FullName}.{methodInfo.Name} " +
                    $"has an invalid signature. " +
                    $"A single parameter of type IMessage or drived type is expected.");
            }

            return messageType;
        }

        private Task ForwardMessage(SubscriberMethod method, IMessage message)
        {
            _logger.LogTrace($"Invoking {method.MethodInfo.DeclaringType.FullName}.{method.MethodInfo.Name}...");

            var result = method.MethodInfo.Invoke(method.Instance, new[] {message});

            return method.MethodInfo.IsAsync() ? (Task) result : Task.CompletedTask;
        }

        private class SubscriberMethod
        {
            public TSubscriber Instance { get; set; }
            public MethodInfo MethodInfo { get; set; }
            public Type SubscribedMessageType { get; set; }
        }
    }
}