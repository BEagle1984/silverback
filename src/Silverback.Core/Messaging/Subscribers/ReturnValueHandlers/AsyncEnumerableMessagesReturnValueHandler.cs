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
    private readonly BusOptions _busOptions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncEnumerableMessagesReturnValueHandler" /> class.
    /// </summary>
    /// <param name="busOptions">
    ///     The <see cref="BusOptions" /> that specify which message types have to be handled.
    /// </param>
    public AsyncEnumerableMessagesReturnValueHandler(BusOptions busOptions)
    {
        _busOptions = busOptions;
    }

    /// <inheritdoc cref="IReturnValueHandler.CanHandle" />
    public bool CanHandle(object returnValue) =>
        returnValue != null &&
        returnValue.GetType().GetInterfaces().Any(
            type => type.IsGenericType &&
                 type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) &&
                 _busOptions.MessageTypes.Any(
                     messageType =>
                         messageType.IsAssignableFrom(type.GenericTypeArguments[0])));

    /// <inheritdoc cref="IReturnValueHandler.Handle" />
    public void Handle(IPublisher publisher, object returnValue)
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(returnValue, nameof(returnValue));

        ((IAsyncEnumerable<object>)returnValue).ForEachAsync(message => publisher.PublishAsync(message)).SafeWait();
    }

    /// <inheritdoc cref="IReturnValueHandler.HandleAsync" />
    public ValueTask HandleAsync(IPublisher publisher, object returnValue)
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(returnValue, nameof(returnValue));

        return ((IAsyncEnumerable<object>)returnValue).ForEachAsync(message => publisher.PublishAsync(message));
    }
}
