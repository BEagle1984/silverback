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
///     Handles the returned <see cref="IAsyncEnumerable{T}" /> republishing all the messages.
/// </summary>
public class AsyncEnumerableMessagesReturnValueHandler : IReturnValueHandler, IMessagesReturnValueHandler, IResultsForwardingReturnValueHandler
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
        returnValue != null && CanHandleType(returnValue.GetType());

    /// <inheritdoc cref="IReturnValueHandler.Handle" />
    public void Handle(IPublisher publisher, object returnValue)
    {
        Check.NotNull(publisher);
        Check.NotNull(returnValue);

        ((IAsyncEnumerable<object>)returnValue).ForEachAsync(message => publisher.PublishAsync(message)).SafeWait();
    }

    /// <inheritdoc cref="IReturnValueHandler.HandleAsync" />
    public ValueTask HandleAsync(IPublisher publisher, object returnValue)
    {
        Check.NotNull(publisher);
        Check.NotNull(returnValue);

        return ((IAsyncEnumerable<object>)returnValue).ForEachAsync(message => publisher.PublishAsync(message));
    }

    /// <inheritdoc cref="IMessagesReturnValueHandler.CanHandleReturnType" />
    bool IMessagesReturnValueHandler.CanHandleReturnType(Type returnType) => CanHandleType(Check.NotNull(returnType));

    /// <inheritdoc cref="IResultsForwardingReturnValueHandler.HandleAndGetResults" />
    IReadOnlyCollection<object?> IResultsForwardingReturnValueHandler.HandleAndGetResults(
        Publisher publisher,
        object returnValue,
        Type resultType) =>
        ((IResultsForwardingReturnValueHandler)this).HandleAndGetResultsAsync(publisher, returnValue, resultType).SafeWait();

    /// <inheritdoc cref="IResultsForwardingReturnValueHandler.HandleAndGetResultsAsync" />
    async ValueTask<IReadOnlyCollection<object?>> IResultsForwardingReturnValueHandler.HandleAndGetResultsAsync(
        Publisher publisher,
        object returnValue,
        Type resultType)
    {
        Check.NotNull(publisher);
        Check.NotNull(returnValue);

        List<object?> results = [];

        await foreach (object message in ((IAsyncEnumerable<object>)returnValue).ConfigureAwait(false))
        {
            results.AddRange(await publisher.RepublishAsync(message, resultType).ConfigureAwait(false));
        }

        return results;
    }

    private bool CanHandleType(Type type) =>
        MessagesReturnTypeHelper.IsMessagesCollection(type, typeof(IAsyncEnumerable<>), _busOptions.MessageTypes);
}
