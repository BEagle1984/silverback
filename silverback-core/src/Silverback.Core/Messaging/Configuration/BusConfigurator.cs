// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers.Subscriptions;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test
    public class BusConfigurator
    {
        private readonly BusOptions _busOptions;

        public BusConfigurator(BusOptions busOptions)
        {
            _busOptions = busOptions;
        }

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

        public BusConfigurator Subscribe(Delegate handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Action<TMessage> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<TMessage, Task> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<TMessage, object> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<TMessage, Task<object>> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Action<IEnumerable<TMessage>> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }
        public BusConfigurator Subscribe<TMessage>(Func<IEnumerable<TMessage>, Task> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<IEnumerable<TMessage>, object> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<IEnumerable<TMessage>, Task<object>> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        #endregion

        #region Subscribe (delegate w/ service provider)

        public BusConfigurator Subscribe<TMessage>(Action<TMessage, IServiceProvider> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<TMessage, IServiceProvider, object> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Action<IEnumerable<TMessage>, IServiceProvider> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        public BusConfigurator Subscribe<TMessage>(Func<IEnumerable<TMessage>, IServiceProvider, object> handler)
        {
            _busOptions.Subscriptions.Add(new DelegateSubscription(handler));
            return this;
        }

        #endregion

        #region Subscribe (annotation based)

        public BusConfigurator Subscribe<TSubscriber>() =>
            Subscribe(typeof(TSubscriber));

        public BusConfigurator Subscribe(Type subscriberType)
        {
            _busOptions.Subscriptions.Add(new AnnotationBasedSubscription(subscriberType));
            return this;
        }

        #endregion
    }
}
