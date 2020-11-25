// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    /// <summary>
    ///     Handles the returned <see cref="IReadOnlyCollection{T}" /> republishing all the messages.
    /// </summary>
    public class ReadOnlyCollectionMessagesReturnValueHandler : IReturnValueHandler
    {
        private readonly IBusOptions _busOptions;

        private readonly IPublisher _publisher;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReadOnlyCollectionMessagesReturnValueHandler" /> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" /> to be used to publish the messages.
        /// </param>
        /// <param name="busOptions">
        ///     The <see cref="IBusOptions" /> that specify which message types have to be handled.
        /// </param>
        public ReadOnlyCollectionMessagesReturnValueHandler(IPublisher publisher, IBusOptions busOptions)
        {
            _publisher = publisher;
            _busOptions = busOptions;
        }

        /// <inheritdoc cref="IReturnValueHandler.CanHandle" />
        public bool CanHandle(object returnValue) =>
            returnValue != null &&
            returnValue.GetType().GetInterfaces().Any(
                i => i.IsGenericType &&
                     i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>) &&
                     _busOptions.MessageTypes.Any(
                         messageType =>
                             messageType.IsAssignableFrom(i.GenericTypeArguments[0])));

        /// <inheritdoc cref="IReturnValueHandler.Handle" />
        public void Handle(object returnValue) =>
            _publisher.Publish<object>((IReadOnlyCollection<object>)returnValue);

        /// <inheritdoc cref="IReturnValueHandler.HandleAsync" />
        public Task HandleAsync(object returnValue) =>
            _publisher.PublishAsync<object>((IReadOnlyCollection<object>)returnValue);
    }
}
