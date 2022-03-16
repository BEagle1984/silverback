// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

    [SuppressMessage("", "VSTHRD103", Justification = Justifications.ExecutesSyncOrAsync)]
    public async ValueTask<bool> HandleReturnValuesAsync(object? returnValue, bool executeAsync)
    {
        if (returnValue == null || returnValue.GetType().Name == "VoidTaskResult")
            return false;

        IReturnValueHandler? returnValueHandler = _returnValueHandlers.FirstOrDefault(handler => handler.CanHandle(returnValue));

        if (returnValueHandler != null)
        {
            if (executeAsync)
                await returnValueHandler.HandleAsync(returnValue).ConfigureAwait(false);
            else
                returnValueHandler.Handle(returnValue);

            return true;
        }

        return false;
    }
}
