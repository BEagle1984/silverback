using System;
using System.Collections.Concurrent;
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
        private ConcurrentDictionary<Type, AnnotatedMethod[]> _methodsCache;
        private const bool UseCache = true; // TODO: Test performance gain and/or clean it up

        public SubscriberFactory(ITypeFactory typeFactory)
        {
            _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
        }
        
        public override void Init(IBus bus)
        {
            base.Init(bus);
            _logger = bus.GetLoggerFactory().CreateLogger<SubscriberFactory<TSubscriber>>();
            _methodsCache = (ConcurrentDictionary<Type, AnnotatedMethod[]>) bus.Items.GetOrAdd(
                $"Silverback.Messaging.Subscribers.AnnotatedMethods<{typeof(TSubscriber).AssemblyQualifiedName}>",
                _ => new ConcurrentDictionary<Type, AnnotatedMethod[]>());
        }

        public override Task HandleAsync(IMessage message) =>
            GetAllAnnotatedMethods()
                .Where(method => method.SubscribedMessageType.IsInstanceOfType(message))
                .ForEachAsync(method => ForwardMessage(method, message));

        private AnnotatedMethod[] GetAllAnnotatedMethods()
            => GetSubscriberInstancesFromTypeFactory()
                .SelectMany(GetAnnotatedMethods)
                .ToArray();

        private TSubscriber[] GetSubscriberInstancesFromTypeFactory()
        {
            var subscribers = _typeFactory.GetInstances<TSubscriber>() ?? Array.Empty<TSubscriber>();

            _logger.LogTrace($"Resolved {subscribers.Length} object(s) of type '{typeof(TSubscriber).Name}'.");

            return subscribers;
        }

        private IEnumerable<AnnotatedMethod> GetAnnotatedMethods(TSubscriber subscriber) =>
            GetAnnotatedMethods(subscriber.GetType())
                .Select(method =>
                {
                    method.Instance = subscriber;
                    return method;
                });

        private AnnotatedMethod[] GetAnnotatedMethods(Type type)
            => _methodsCache.GetOrAdd(type, t =>
                t.GetAnnotatedMethods<SubscribeAttribute>()
                    .Select(methodInfo =>
                        new AnnotatedMethod
                        {
                            MethodInfo = methodInfo,
                            SubscribedMessageType = GetMessageParameterType(methodInfo)
                        })
                    .ToArray());

        private Type GetMessageParameterType(MethodInfo methodInfo)
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

        private Task ForwardMessage(AnnotatedMethod method, IMessage message)
        {
            _logger.LogTrace($"Invoking {method.MethodInfo.DeclaringType.FullName}.{method.MethodInfo.Name}...");

            var result = method.MethodInfo.Invoke(method.Instance, new[] {message});

            return method.MethodInfo.IsAsync() ? (Task) result : Task.CompletedTask;
        }

        private class AnnotatedMethod
        {
            public TSubscriber Instance { get; set; }
            public MethodInfo MethodInfo { get; set; }
            public Type SubscribedMessageType { get; set; }
        }
    }
}