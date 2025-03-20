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
///     Handles the returned <see cref="IEnumerable{T}" /> republishing all the messages.
/// </summary>
public class EnumerableMessagesReturnValueHandler : IReturnValueHandler
{
    private readonly BusOptions _busOptions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EnumerableMessagesReturnValueHandler" /> class.
    /// </summary>
    /// <param name="busOptions">
    ///     The <see cref="BusOptions" /> that specify which message types have to be handled.
    /// </param>
    public EnumerableMessagesReturnValueHandler(BusOptions busOptions)
    {
        _busOptions = busOptions;
    }

    /// <inheritdoc cref="IReturnValueHandler.CanHandle" />
    public bool CanHandle(object returnValue) =>
        returnValue != null &&
        returnValue.GetType().GetInterfaces().Any(
            type => type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                    _busOptions.MessageTypes.Any(
                        messageType =>
                            messageType.IsAssignableFrom(type.GenericTypeArguments[0])));

    /// <inheritdoc cref="IReturnValueHandler.Handle" />
    public void Handle(IPublisher publisher, object returnValue)
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(returnValue, nameof(returnValue));

        ((IEnumerable<object>)returnValue).ForEach(message => publisher.Publish(message));
    }

    /// <inheritdoc cref="IReturnValueHandler.HandleAsync" />
    public ValueTask HandleAsync(IPublisher publisher, object returnValue)
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(returnValue, nameof(returnValue));

        return ((IEnumerable<object>)returnValue).ForEachAsync(message => publisher.PublishAsync(message));
    }
}
