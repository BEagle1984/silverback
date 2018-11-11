using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    public class SubscriberFactory<TSubscriber> : AsyncSubscriber<IMessage>
    {
        private readonly ITypeFactory _typeFactory;
        private IBus _bus;
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
            _bus = bus;
            _logger = bus.GetLoggerFactory().CreateLogger<SubscriberFactory<TSubscriber>>();
            _methodsCache = (ConcurrentDictionary<Type, AnnotatedMethod[]>) bus.Items.GetOrAdd(
                $"Silverback.Messaging.Subscribers.AnnotatedMethods<{typeof(TSubscriber).AssemblyQualifiedName}>",
                _ => new ConcurrentDictionary<Type, AnnotatedMethod[]>());
        }

        public override Task HandleAsync(IMessage message) =>
            GetAllAnnotatedMethods()
                .Where(method => method.SubscribedMessageType.IsInstanceOfType(message))
                .ForEachAsync(method => InvokeAnnotatedMethod(method, message));

        private AnnotatedMethod[] GetAllAnnotatedMethods()
            => GetSubscriberInstancesFromTypeFactory()
                .SelectMany(GetAnnotatedMethods)
                .ToArray();

        private TSubscriber[] GetSubscriberInstancesFromTypeFactory()
        {
            var subscribers = _typeFactory.GetInstances<TSubscriber>()?.ToArray() ?? Array.Empty<TSubscriber>();

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
                    {
                        var parameters = methodInfo.GetParameters();
                        return new AnnotatedMethod
                        {
                            MethodInfo = methodInfo,
                            Parameters = parameters,
                            SubscribedMessageType = GetMessageParameterType(methodInfo, parameters)
                        };
                    })
                    .ToArray());

        private Type GetMessageParameterType(MethodInfo methodInfo, ParameterInfo[] parameters)
        {
            var messageParameters = parameters.Where(p => typeof(IMessage).IsAssignableFrom(p.ParameterType)).ToArray();

            if (messageParameters.Length != 1)
                ThrowMethodSignatureException(methodInfo, "A single parameter of type IMessage or drived type is expected.");

            return parameters.First().ParameterType;
        }

        private void ThrowMethodSignatureException(MethodInfo methodInfo, string message) =>
            throw new SilverbackException(
                $"The method {methodInfo.DeclaringType.FullName}.{methodInfo.Name} " +
                $"has an invalid signature. {message}");

        private Task InvokeAnnotatedMethod(AnnotatedMethod method, IMessage message)
        {
            _logger.LogTrace($"Invoking {method.MethodInfo.DeclaringType.FullName}.{method.MethodInfo.Name}...");

            var result = method.MethodInfo.Invoke(method.Instance, GetMethodParameterValues(method.Parameters, message));

            return method.MethodInfo.IsAsync() ? (Task) result : Task.CompletedTask;
        }

        private object[] GetMethodParameterValues(ParameterInfo[] parameters, IMessage message)
            => parameters.Length == 1
                ? new object[] {message}
                : parameters.MapParameterValues(new Dictionary<Type, Func<Type, object>>
                {
                    {typeof(IMessage), _ => message},
                    {typeof(IBus), _ => _bus},
                    {typeof(IPublisher), _ => _bus.GetPublisher()}
                });

        private class AnnotatedMethod
        {
            public TSubscriber Instance { get; set; }
            public MethodInfo MethodInfo { get; set; }
            public ParameterInfo[] Parameters { get; set; }
            public Type SubscribedMessageType { get; set; }
        }
    }
}