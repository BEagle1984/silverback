// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Handles the returned <see cref="IAsyncEnumerable{T}" /> republishing all the messages.
/// </summary>
public class AsyncEnumerableMessagesReturnValueHandler : IReturnValueHandler
{
    private readonly MediatorOptions _mediatorOptions;

    private readonly IPublisher _publisher;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncEnumerableMessagesReturnValueHandler" /> class.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" /> to be used to publish the messages.
    /// </param>
    /// <param name="mediatorOptions">
    ///     The <see cref="MediatorOptions" /> that specify which message types have to be handled.
    /// </param>
    public AsyncEnumerableMessagesReturnValueHandler(IPublisher publisher, MediatorOptions mediatorOptions)
    {
        _publisher = publisher;
        _mediatorOptions = mediatorOptions;
    }

    /// <inheritdoc cref="IReturnValueHandler.CanHandle" />
    public bool CanHandle(object returnValue) =>
        returnValue != null &&
        returnValue.GetType().GetInterfaces().Any(
            i => i.IsGenericType &&
                 i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) &&
                 _mediatorOptions.MessageTypes.Any(
                     messageType =>
                         messageType.IsAssignableFrom(i.GenericTypeArguments[0])));

    /// <inheritdoc cref="IReturnValueHandler.Handle" />
    public void Handle(object returnValue)
    {
        Check.NotNull(returnValue, nameof(returnValue));

        ((IAsyncEnumerable<object>)returnValue).ForEachAsync(message => _publisher.PublishAsync(message)).SafeWait();
    }

    /// <inheritdoc cref="IReturnValueHandler.HandleAsync" />
    public ValueTask HandleAsync(object returnValue)
    {
        Check.NotNull(returnValue, nameof(returnValue));

        return ((IAsyncEnumerable<object>)returnValue).ForEachAsync(message => _publisher.PublishAsync(message));
    }
}
