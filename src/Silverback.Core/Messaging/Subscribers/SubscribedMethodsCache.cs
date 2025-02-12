// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers;

internal sealed class SubscribedMethodsCache
{
    private readonly ConcurrentDictionary<Type, IReadOnlyList<SubscribedMethod>> _exclusiveMethodsCache = [];

    private readonly ConcurrentDictionary<Type, IReadOnlyList<SubscribedMethod>> _nonExclusiveMethodsCache = [];

    private readonly ConcurrentDictionary<Type, bool> _hasMessageStreamSubscriber = [];

    private readonly MediatorOptions _options;

    private bool? _hasAnyMessageStreamSubscriber;

    private IReadOnlyCollection<SubscribedMethod>? _subscribedMethods;

    public SubscribedMethodsCache(MediatorOptions options)
    {
        _options = Check.NotNull(options, nameof(options));
    }

    public IReadOnlyList<SubscribedMethod> GetExclusiveMethods(object message, IServiceProvider serviceProvider) =>
        GetMethods(message, true, serviceProvider);

    public IReadOnlyList<SubscribedMethod> GetNonExclusiveMethods(object message, IServiceProvider serviceProvider) =>
        GetMethods(message, false, serviceProvider);

    public bool HasMessageStreamSubscriber(object message, IServiceProvider serviceProvider) =>
        HasAnyMessageStreamSubscriber(serviceProvider) &&
        _hasMessageStreamSubscriber.GetOrAdd(
            message.GetType(),
            static (_, args) =>
                args.Instance.GetAllSubscribedMethods(args.ServiceProvider)
                    .Any(
                        method =>
                            method.MessageArgumentResolver is IStreamEnumerableMessageArgumentResolver &&
                            WouldBeCompatibleWithMessageStream(args.Message, method)),
            (ServiceProvider: serviceProvider, Message: message, Instance: this));

    public void Preload(IServiceProvider serviceProvider) => HasAnyMessageStreamSubscriber(serviceProvider); // Internally calls GetAllSubscribedMethods

    private static bool AreCompatible(object message, SubscribedMethod subscribedMethod)
    {
        if (subscribedMethod.MessageArgumentResolver is IStreamEnumerableMessageArgumentResolver)
            return AreCompatibleStreams(message, subscribedMethod);

        if (message is IEnvelope envelope && subscribedMethod.MessageType.IsAssignableFrom(envelope.MessageType))
            return true;

        if (subscribedMethod.MessageType.IsInstanceOfType(message))
            return true;

        return false;
    }

    private static bool AreCompatibleStreams(object message, SubscribedMethod subscribedMethod)
    {
        if (message is not IMessageStreamProvider streamProvider)
            return false;

        // There is no way to properly match the message types in the case of a stream of IEnvelope
        // and a subscriber that is not handling a stream of envelopes. The envelopes can contain any
        // type of message (object? Message) and will automatically be unwrapped, filtered and properly
        // routed by the MessageStreamEnumerable.
        if (typeof(IEnvelope).IsAssignableFrom(streamProvider.MessageType) &&
            !typeof(IEnvelope).IsAssignableFrom(subscribedMethod.MessageType))
        {
            return true;
        }

        if (streamProvider.MessageType.IsAssignableFrom(subscribedMethod.MessageType) ||
            subscribedMethod.MessageType.IsAssignableFrom(streamProvider.MessageType))
        {
            return true;
        }

        return false;
    }

    private static bool WouldBeCompatibleWithMessageStream(object message, SubscribedMethod subscribedMethod) =>
        message is IEnvelope envelope &&
        (subscribedMethod.MessageType.IsAssignableFrom(envelope.MessageType) ||
         subscribedMethod.MessageType.IsInstanceOfType(envelope));

    private IReadOnlyList<SubscribedMethod> GetMethods(object message, bool exclusive, IServiceProvider serviceProvider) =>
        (exclusive ? _exclusiveMethodsCache : _nonExclusiveMethodsCache)
        .GetOrAdd(
            message.GetType(),
            static (_, args) => args.Instance.GetAllSubscribedMethods(args.ServiceProvider)
                .Where(
                    subscribedMethod => AreCompatible(args.Message, subscribedMethod) &&
                                        subscribedMethod.Options.IsExclusive == args.Exclusive)
                .ToList(),
            (ServiceProvider: serviceProvider, Message: message, Exclusive: exclusive, Instance: this));

    private IReadOnlyCollection<SubscribedMethod> GetAllSubscribedMethods(IServiceProvider serviceProvider) =>
        _subscribedMethods ??= LoadSubscribedMethods(serviceProvider);

    private List<SubscribedMethod> LoadSubscribedMethods(IServiceProvider serviceProvider) =>
        _options.Subscriptions
            .SelectMany(subscription => subscription.GetSubscribedMethods(serviceProvider))
            .ToList();

    private bool HasAnyMessageStreamSubscriber(IServiceProvider serviceProvider) =>
        _hasAnyMessageStreamSubscriber ??=
            GetAllSubscribedMethods(serviceProvider).Any(method => method.MessageArgumentResolver is IStreamEnumerableMessageArgumentResolver);
}
