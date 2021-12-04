// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Subscribers;

internal sealed class MethodInvocationResult
{
    private MethodInvocationResult(bool wasInvoked)
    {
        WasInvoked = wasInvoked;
    }

    private MethodInvocationResult(object returnValue)
    {
        WasInvoked = true;
        ReturnValue = returnValue;
    }

    public static MethodInvocationResult NotInvoked { get; } =
        new(false);

    public static MethodInvocationResult Invoked { get; } =
        new(true);

    public bool WasInvoked { get; }

    public object? ReturnValue { get; }

    public static MethodInvocationResult WithReturnValue(object returnValue) => new(returnValue);
}
