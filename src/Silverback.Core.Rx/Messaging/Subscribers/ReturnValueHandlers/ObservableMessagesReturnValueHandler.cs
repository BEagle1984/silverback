// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    // TODO: Test
    public class ObservableMessagesReturnValueHandler : IReturnValueHandler
    {
        private readonly IPublisher _publisher;
        private readonly BusOptions _publisherOptions;

        public ObservableMessagesReturnValueHandler(IPublisher publisher, BusOptions publisherOptions)
        {
            _publisher = publisher;
            _publisherOptions = publisherOptions;
        }

        public bool CanHandle(object returnValue) =>
            returnValue != null &&
            returnValue.GetType().GetInterfaces().Any(
                i => i.IsGenericType &&
                     i.GetGenericTypeDefinition() == typeof(IObservable<>) &&
                     _publisherOptions.MessageTypes.Any(messageType => messageType.IsAssignableFrom(i.GenericTypeArguments[0])));

        public IEnumerable<object> Handle(object returnValue) =>
             _publisher.Publish<object>(((IObservable<object>)returnValue).ToEnumerable());

        public Task<IEnumerable<object>> HandleAsync(object returnValue) =>
            _publisher.PublishAsync<object>(((IObservable<object>)returnValue).ToEnumerable());
    }
}