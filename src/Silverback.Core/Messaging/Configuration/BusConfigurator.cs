// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers.Subscriptions;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test
    public class BusConfigurator
    {
        private readonly BusOptions _busOptions;

        public BusConfigurator(BusOptions busOptions, IServiceProvider serviceProvider)
        {
            _busOptions = busOptions;
            ServiceProvider = serviceProvider;
        }

        internal IServiceProvider ServiceProvider { get; }

        #region HandleMessagesOfType

        public BusConfigurator HandleMessagesOfType<TMessage>() =>
            HandleMessagesOfType(typeof(TMessage));

        public BusConfigurator HandleMessagesOfType(Type messageType)
        {
            if (!_busOptions.MessageTypes.Contains(messageType))
                _busOptions.MessageTypes.Add(messageType);

            return this;
        }

        #endregion

        #region Subscribe (delegate)

        public BusConfigurator Subscribe(Delegate handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Action<TMessage> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<TMessage, Task> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<TMessage, object> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<TMessage, Task<object>> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Action<IEnumerable<TMessage>> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<IEnumerable<TMessage>, Task> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<IEnumerable<TMessage>, object> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<IEnumerable<TMessage>, Task<object>> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        #endregion

        #region Subscribe (delegate w/ service provider)

        public BusConfigurator Subscribe<TMessage>(Action<TMessage, IServiceProvider> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<TMessage, IServiceProvider, object> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Action<IEnumerable<TMessage>, IServiceProvider> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<IEnumerable<TMessage>, IServiceProvider, object> handler, SubscriptionOptions options = null)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler, options));
            return this;
        }

        #endregion

        #region Subscribe (annotation based)

        public BusConfigurator Subscribe<TSubscriber>(bool autoSubscribeAllPublicMethods = true) =>
            Subscribe(typeof(TSubscriber), autoSubscribeAllPublicMethods);

        public BusConfigurator Subscribe(Type subscriberType, bool autoSubscribeAllPublicMethods = true)
        {
            var previousSubscription = _busOptions.Subscriptions.OfType<TypeSubscription>()
                .FirstOrDefault(sub => sub.SubscriberType == subscriberType);

            if (previousSubscription != null)
                _busOptions.Subscriptions.Remove(previousSubscription);

            _busOptions.Subscriptions.Add(new TypeSubscription(subscriberType, autoSubscribeAllPublicMethods));
            return this;
        }

        #endregion
    }
}
