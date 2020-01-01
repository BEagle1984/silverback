// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    // TODO: Test
    public class ReadOnlyCollectionMessagesReturnValueHandler : IReturnValueHandler
    {
        private readonly IPublisher _publisher;
        private readonly BusOptions _publisherOptions;

        public ReadOnlyCollectionMessagesReturnValueHandler(IPublisher publisher, BusOptions publisherOptions)
        {
            _publisher = publisher;
            _publisherOptions = publisherOptions;
        }

        public bool CanHandle(object returnValue) =>
            returnValue != null &&
            returnValue.GetType().GetInterfaces().Any(
                i => i.IsGenericType &&
                     i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>) &&
                     _publisherOptions.MessageTypes.Any(messageType =>
                         messageType.IsAssignableFrom(i.GenericTypeArguments[0])));

        public void Handle(object returnValue) =>
            _publisher.Publish<object>((IReadOnlyCollection<object>) returnValue);

        public Task HandleAsync(object returnValue) =>
            _publisher.PublishAsync<object>((IReadOnlyCollection<object>) returnValue);
    }
}