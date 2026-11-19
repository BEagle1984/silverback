// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Implemented (explicitly) by the built-in <see cref="IReturnValueHandler" /> implementations that republish the returned messages.
///     The results of the republished messages become the results of the subscriber that returned them. The custom
///     <see cref="IReturnValueHandler" /> implementations don't implement it and don't contribute any result, and neither do the
///     subclasses re-implementing the <see cref="IReturnValueHandler" /> Handle or HandleAsync of a built-in handler.
/// </summary>
internal interface IResultsForwardingReturnValueHandler
{
    /// <summary>
    ///     Republishes the messages contained in the specified return value and returns the results of their subscribers, blocking the
    ///     calling thread until they are all republished, so that the synchronous publish of the original message never continues on
    ///     another thread (e.g. losing the ambient transaction). Like <see cref="IReturnValueHandler.Handle" />, the implementation
    ///     decides how each message is republished: the handlers of the synchronously enumerated messages republish and block for each
    ///     message on the calling thread, while the <see cref="AsyncEnumerableMessagesReturnValueHandler" /> enumerates and
    ///     republishes the messages asynchronously and blocks only once for the whole enumeration.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="Publisher" /> that published the message.
    /// </param>
    /// <param name="returnValue">
    ///     The value to be handled.
    /// </param>
    /// <param name="resultType">
    ///     The result type expected by the original publisher, used to discard the incompatible <c>null</c> results.
    /// </param>
    /// <returns>
    ///     The (uncast) results of the subscribers of the republished messages.
    /// </returns>
    IReadOnlyCollection<object?> HandleAndGetResults(Publisher publisher, object returnValue, Type resultType);

    /// <summary>
    ///     Republishes the messages contained in the specified return value asynchronously and returns the results of their subscribers.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="Publisher" /> that published the message.
    /// </param>
    /// <param name="returnValue">
    ///     The value to be handled.
    /// </param>
    /// <param name="resultType">
    ///     The result type expected by the original publisher, used to discard the incompatible <c>null</c> results.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the (uncast) results of
    ///     the subscribers of the republished messages.
    /// </returns>
    ValueTask<IReadOnlyCollection<object?>> HandleAndGetResultsAsync(Publisher publisher, object returnValue, Type resultType);
}
