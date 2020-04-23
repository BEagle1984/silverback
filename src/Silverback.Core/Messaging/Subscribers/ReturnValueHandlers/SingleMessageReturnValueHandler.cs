// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    /// <summary>
    ///     Handles the returned message republishing it.
    /// </summary>
    // TODO: Test
    public class SingleMessageReturnValueHandler : IReturnValueHandler
    {
        private readonly IPublisher _publisher;
        private readonly BusOptions _busOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleMessageReturnValueHandler"/> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" /> to be used to publish the messages.
        /// </param>
        /// <param name="busOptions">
        ///     The <see cref="BusOptions" /> that specify which message types have to be handled.
        /// </param>
        public SingleMessageReturnValueHandler(IPublisher publisher, BusOptions busOptions)
        {
            _publisher = publisher;
            _busOptions = busOptions;
        }

        /// <inheritdoc />
        public bool CanHandle(object returnValue) =>
            returnValue != null &&
            _busOptions.MessageTypes.Any(t => t.IsInstanceOfType(returnValue));

        /// <inheritdoc />
        public void Handle(object returnValue) =>
            _publisher.Publish<object>(returnValue);

        /// <inheritdoc />
        public Task HandleAsync(object returnValue) =>
            _publisher.PublishAsync<object>(returnValue);
    }
}