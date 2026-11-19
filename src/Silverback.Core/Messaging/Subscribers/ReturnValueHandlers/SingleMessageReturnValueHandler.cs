// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Handles the returned message republishing it.
/// </summary>
public class SingleMessageReturnValueHandler : IReturnValueHandler, IMessagesReturnValueHandler, IResultsForwardingReturnValueHandler
{
    private readonly BusOptions _busOptions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SingleMessageReturnValueHandler" /> class.
    /// </summary>
    /// <param name="busOptions">
    ///     The <see cref="BusOptions" /> that specify which message types have to be handled.
    /// </param>
    public SingleMessageReturnValueHandler(BusOptions busOptions)
    {
        _busOptions = busOptions;
    }

    /// <inheritdoc cref="IReturnValueHandler.CanHandle" />
    public bool CanHandle(object returnValue) =>
        returnValue != null && CanHandleType(returnValue.GetType());

    /// <inheritdoc cref="IReturnValueHandler.Handle" />
    public void Handle(IPublisher publisher, object returnValue) =>
        Check.NotNull(publisher).Publish(returnValue);

    /// <inheritdoc cref="IReturnValueHandler.HandleAsync" />
    public async ValueTask HandleAsync(IPublisher publisher, object returnValue) =>
        await Check.NotNull(publisher).PublishAsync(returnValue).ConfigureAwait(false);

    /// <inheritdoc cref="IMessagesReturnValueHandler.CanHandleReturnType" />
    bool IMessagesReturnValueHandler.CanHandleReturnType(Type returnType) => CanHandleType(Check.NotNull(returnType));

    /// <inheritdoc cref="IResultsForwardingReturnValueHandler.HandleAndGetResults" />
    IReadOnlyCollection<object?> IResultsForwardingReturnValueHandler.HandleAndGetResults(
        Publisher publisher,
        object returnValue,
        Type resultType) =>
        Check.NotNull(publisher).Republish(Check.NotNull(returnValue), resultType);

    /// <inheritdoc cref="IResultsForwardingReturnValueHandler.HandleAndGetResultsAsync" />
    ValueTask<IReadOnlyCollection<object?>> IResultsForwardingReturnValueHandler.HandleAndGetResultsAsync(
        Publisher publisher,
        object returnValue,
        Type resultType) =>
        Check.NotNull(publisher).RepublishAsync(Check.NotNull(returnValue), resultType);

    private bool CanHandleType(Type type) => MessagesReturnTypeHelper.IsMessage(type, _busOptions.MessageTypes);
}
