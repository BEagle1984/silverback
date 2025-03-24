// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Handles the returned <see cref="IObservable{T}" /> republishing all the messages.
/// </summary>
public class ObservableMessagesReturnValueHandler : IReturnValueHandler
{
    private readonly BusOptions _busOptions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ObservableMessagesReturnValueHandler" /> class.
    /// </summary>
    /// <param name="busOptions">
    ///     The <see cref="BusOptions" /> that specify which message types have to be handled.
    /// </param>
    public ObservableMessagesReturnValueHandler(BusOptions busOptions)
    {
        _busOptions = busOptions;
    }

    /// <inheritdoc cref="IReturnValueHandler.CanHandle" />
    public bool CanHandle(object returnValue) =>
        returnValue != null &&
        returnValue.GetType().GetInterfaces().Any(
            i => i.IsGenericType &&
                 i.GetGenericTypeDefinition() == typeof(IObservable<>) &&
                 _busOptions.MessageTypes.Any(
                     messageType =>
                         messageType.IsAssignableFrom(i.GenericTypeArguments[0])));

    /// <inheritdoc cref="IReturnValueHandler.Handle" />
    public void Handle(IPublisher publisher, object returnValue) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<object>(((IObservable<object>)returnValue).ToEnumerable());

    /// <inheritdoc cref="IReturnValueHandler.HandleAsync" />
    public async ValueTask HandleAsync(IPublisher publisher, object returnValue) =>
        await Check.NotNull(publisher, nameof(publisher)).PublishAsync<object>(((IObservable<object>)returnValue).ToEnumerable()).ConfigureAwait(false);
}
