// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Implemented (explicitly) by the built-in <see cref="IReturnValueHandler" /> implementations that republish the returned messages,
///     to determine whether a subscriber is declared to return the messages to be republished. A <c>null</c> returned by such a
///     subscriber means that there is nothing to republish, not a result. The custom <see cref="IReturnValueHandler" /> implementations
///     don't implement it and are therefore not taken into account.
/// </summary>
internal interface IMessagesReturnValueHandler
{
    /// <summary>
    ///     Returns a value indicating whether this handler handles any non-null value of the specified type, i.e. whether
    ///     <see cref="IReturnValueHandler.CanHandle" /> returns <c>true</c> for every value of that type.
    /// </summary>
    /// <param name="returnType">
    ///     The result type declared by the subscriber (unwrapped from the <see cref="System.Threading.Tasks.Task{TResult}" /> or the
    ///     <see cref="System.Threading.Tasks.ValueTask{TResult}" />).
    /// </param>
    /// <returns>
    ///     A value indicating whether the values of the specified type are handled.
    /// </returns>
    bool CanHandleReturnType(Type returnType);
}
