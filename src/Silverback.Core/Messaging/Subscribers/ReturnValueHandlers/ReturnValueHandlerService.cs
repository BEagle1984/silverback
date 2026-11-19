// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Calls the registered <see cref="IReturnValueHandler" />'s.
/// </summary>
internal sealed class ReturnValueHandlerService
{
    private readonly IReadOnlyCollection<HandlerEntry> _returnValueHandlers;

    public ReturnValueHandlerService(IEnumerable<IReturnValueHandler> returnValueHandlers)
    {
        // Revert the handlers order, to give priority to the ones added after the
        // default ones.
        _returnValueHandlers = [.. returnValueHandlers.Reverse().Select(handler => new HandlerEntry(handler, GetResultsForwardingHandler(handler)))];
    }

    // A subscriber declared to return a type handled by one of the registered built-in handlers republishing the messages returns the
    // messages to be republished: the null it returns means that there is nothing to republish, not a result. The custom handlers
    // aren't taken into account.
    public bool IsMessagesReturnType(Type returnType) =>
        _returnValueHandlers.Any(entry => entry.Handler is IMessagesReturnValueHandler messagesHandler &&
                                          messagesHandler.CanHandleReturnType(returnType));

    // Returns null if the return value wasn't handled (and is therefore itself the result), otherwise the results of the republished
    // messages (empty for the custom handlers, which don't forward any result, and when no result is expected)
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload", Justification = "Method executes sync or async")]
    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "Method executes sync or async")]
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Method executes sync or async")]
    [SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "The synchronous Handle is called on purpose when the execution flow is sync")]
    public async ValueTask<IReadOnlyCollection<object?>?> HandleReturnValuesAsync(
        Publisher publisher,
        object? returnValue,
        Type? resultType,
        ExecutionFlow executionFlow)
    {
        if (returnValue == null || returnValue.GetType().Name == "VoidTaskResult")
            return null;

        HandlerEntry? entry = _returnValueHandlers.FirstOrDefault(entry => entry.Handler.CanHandle(returnValue));

        if (entry == null)
            return null;

        // The results are forwarded only when the publisher expects them (Publish<TResult>). With a non-generic Publish, the messages are
        // republished through the plain Handle or HandleAsync, discarding the results of each republished message right away instead of
        // collecting them until the whole return value is handled, only to discard them at the end.
        switch (executionFlow)
        {
            case ExecutionFlow.Async when resultType != null && entry.ResultsForwardingHandler != null:
                return await entry.ResultsForwardingHandler
                    .HandleAndGetResultsAsync(publisher, returnValue, resultType)
                    .ConfigureAwait(false);
            case ExecutionFlow.Async:
                await entry.Handler.HandleAsync(publisher, returnValue).ConfigureAwait(false);
                return [];

            // Like the synchronous Handle, it blocks until each message is republished, so that the synchronous publish doesn't continue
            // on another thread (e.g. losing the ambient transaction) when a republishing completes asynchronously
            case ExecutionFlow.Sync when resultType != null && entry.ResultsForwardingHandler != null:
                return entry.ResultsForwardingHandler.HandleAndGetResults(publisher, returnValue, resultType);
            case ExecutionFlow.Sync:
                entry.Handler.Handle(publisher, returnValue);
                return [];
            default:
                throw new ArgumentOutOfRangeException(nameof(executionFlow), executionFlow, "Invalid execution flow.");
        }
    }

    // The results are forwarded only if the handler uses the built-in Handle and HandleAsync of the class implementing the forwarding,
    // since the forwarding mirrors them: a subclass that re-implements IReturnValueHandler is treated as a custom handler
    private static IResultsForwardingReturnValueHandler? GetResultsForwardingHandler(IReturnValueHandler handler)
    {
        if (handler is not IResultsForwardingReturnValueHandler resultsForwardingHandler)
            return null;

        Type handlerType = handler.GetType();

        // The interface is internal, so all its methods are implemented by the same built-in class
        Type? forwardingType = handlerType.GetInterfaceMap(typeof(IResultsForwardingReturnValueHandler)).TargetMethods[0].DeclaringType;
        InterfaceMapping returnValueHandlerMap = handlerType.GetInterfaceMap(typeof(IReturnValueHandler));

        bool usesBuiltInHandleMethods = returnValueHandlerMap.InterfaceMethods
            .Zip(returnValueHandlerMap.TargetMethods)
            .Where(methods => methods.First.Name is nameof(IReturnValueHandler.Handle) or nameof(IReturnValueHandler.HandleAsync))
            .All(methods => methods.Second.DeclaringType == forwardingType);

        return usesBuiltInHandleMethods ? resultsForwardingHandler : null;
    }

    private sealed record HandlerEntry(IReturnValueHandler Handler, IResultsForwardingReturnValueHandler? ResultsForwardingHandler);
}
