// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    // TODO: Test
    public class SingleMessageReturnValueHandler : IReturnValueHandler
    {
        private readonly IPublisher _publisher;
        private readonly BusOptions _publisherOptions;

        public SingleMessageReturnValueHandler(IPublisher publisher, BusOptions publisherOptions)
        {
            _publisher = publisher;
            _publisherOptions = publisherOptions;
        }

        public bool CanHandle(object returnValue) =>
            returnValue != null &&
            _publisherOptions.MessageTypes.Any(t => t.IsInstanceOfType(returnValue));

        public void Handle(object returnValue) =>
            _publisher.Publish<object>(returnValue);

        public Task HandleAsync(object returnValue) =>
            _publisher.PublishAsync<object>(returnValue);
    }
}