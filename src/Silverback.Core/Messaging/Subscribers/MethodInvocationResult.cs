// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers;

internal sealed class MethodInvocationResult
{
    private MethodInvocationResult(bool wasInvoked)
    {
        WasInvoked = wasInvoked;
    }

    private MethodInvocationResult(object? returnValue, Type? declaredResultType)
    {
        WasInvoked = true;
        HasReturnValue = true;
        ReturnValue = returnValue;
        DeclaredResultType = declaredResultType;
    }

    public static MethodInvocationResult NotInvoked { get; } =
        new(false);

    /// <summary>
    ///     Gets the result of a method that was invoked but has no return value to be forwarded to the publisher, either because it
    ///     doesn't return a value (void, <see cref="System.Threading.Tasks.Task" />, or <see cref="System.Threading.Tasks.ValueTask" />)
    ///     or because it returns the messages to be republished, which are consumed by a return value handler.
    /// </summary>
    public static MethodInvocationResult Invoked { get; } =
        new(true);

    public bool WasInvoked { get; }

    /// <summary>
    ///     Gets a value indicating whether the method returned a value to be forwarded to the publisher. The value itself may be
    ///     <c>null</c>.
    /// </summary>
    public bool HasReturnValue { get; }

    public object? ReturnValue { get; }

    /// <summary>
    ///     Gets the result type declared by the method (unwrapped from the <see cref="System.Threading.Tasks.Task{TResult}" /> or the
    ///     <see cref="System.Threading.Tasks.ValueTask{TResult}" />). It is set only when the <see cref="ReturnValue" /> is <c>null</c>,
    ///     to determine whether the <c>null</c> result is compatible with the result type expected by the publisher.
    /// </summary>
    public Type? DeclaredResultType { get; }

    public static MethodInvocationResult WithReturnValue(object returnValue) => new(returnValue, null);

    public static MethodInvocationResult WithNullReturnValue(Type declaredResultType) => new(null, declaredResultType);
}
