// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Calls the registered <see cref="IReturnValueHandler" />'s.
/// </summary>
internal sealed class ReturnValueHandlerService
{
    private readonly IReadOnlyCollection<IReturnValueHandler> _returnValueHandlers;

    public ReturnValueHandlerService(IEnumerable<IReturnValueHandler> returnValueHandlers)
    {
        // Revert the handlers order, to give priority to the ones added after the
        // default ones.
        _returnValueHandlers = returnValueHandlers.Reverse().ToList();
    }

    [SuppressMessage("ReSharper", "MethodHasAsyncOverload", Justification = "Method executes sync or async")]
    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "Method executes sync or async")]
    public async ValueTask<bool> HandleReturnValuesAsync(object? returnValue, ExecutionFlow executionFlow)
    {
        if (returnValue == null || returnValue.GetType().Name == "VoidTaskResult")
            return false;

        IReturnValueHandler? returnValueHandler = _returnValueHandlers.FirstOrDefault(handler => handler.CanHandle(returnValue));

        if (returnValueHandler == null)
            return false;

        switch (executionFlow)
        {
            case ExecutionFlow.Async:
                await returnValueHandler.HandleAsync(returnValue).ConfigureAwait(false);
                break;
            case ExecutionFlow.Sync:
                returnValueHandler.Handle(returnValue);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(executionFlow), executionFlow, "Invalid execution flow.");
        }

        return true;
    }
}
