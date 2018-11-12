using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    public class Publisher : IPublisher
    {
        private readonly IEnumerable<ISubscriber> _subscribers;
        private readonly ILogger<Publisher> _logger;

        private static readonly ConcurrentDictionary<Type, AnnotatedMethod[]> MethodsCache = new ConcurrentDictionary<Type, AnnotatedMethod[]>();

        public Publisher(IEnumerable<ISubscriber> subscribers, ILogger<Publisher> logger)
        {
            _subscribers = subscribers;
            _logger = logger;
        }

        public void Publish<TMessage>(TMessage message) where TMessage : IMessage =>
            GetSubscribedMethods(message)
                .ForEach(method => InvokeAnnotatedMethod(method, message, false));

        public Task PublishAsync<TMessage>(TMessage message) where TMessage : IMessage =>
            GetSubscribedMethods(message)
                .ForEachAsync(method => InvokeAnnotatedMethod(method, message, true));

        private AnnotatedMethod[] GetSubscribedMethods<TMessage>(TMessage message) =>
            _subscribers
                .SelectMany(GetAnnotatedMethods)
                .Where(method => method.SubscribedMessageType.IsInstanceOfType(message))
                .ToArray();

        private IEnumerable<AnnotatedMethod> GetAnnotatedMethods(ISubscriber subscriber) =>
            GetAnnotatedMethods(subscriber.GetType())
                .Select(method =>
                {
                    method.Instance = subscriber;
                    return method;
                });

        private AnnotatedMethod[] GetAnnotatedMethods(Type type)
            => MethodsCache.GetOrAdd(type, t =>
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

        private Task InvokeAnnotatedMethod(AnnotatedMethod method, IMessage message, bool executeAsync)
        {
            _logger.LogTrace($"Invoking {method.MethodInfo.DeclaringType.FullName}.{method.MethodInfo.Name}...");

            if (executeAsync)
            {
                var result = InvokeAnnotatedMethod(method, message);
                return method.MethodInfo.IsAsync() ? (Task)result : Task.CompletedTask;
            }
            else
            {
                if (method.MethodInfo.IsAsync())
                    AsyncHelper.RunSynchronously(() => (Task)InvokeAnnotatedMethod(method, message));
                else
                    InvokeAnnotatedMethod(method, message);

                return Task.CompletedTask;
            }
        }

        private object InvokeAnnotatedMethod(AnnotatedMethod method, IMessage message) =>
            method.MethodInfo.Invoke(method.Instance, new object[] { message });
        
        private class AnnotatedMethod
        {
            public ISubscriber Instance { get; set; }
            public MethodInfo MethodInfo { get; set; }
            public ParameterInfo[] Parameters { get; set; }
            public Type SubscribedMessageType { get; set; }
        }
    }
}
