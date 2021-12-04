// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers;

internal static class MethodInvokerExtensions
{
    public static Task<object?> InvokeWithActivityAsync(this MethodInfo methodInfo, object? target, object?[] parameters, bool executeAsync) =>
        executeAsync
            ? InvokeWithActivityAsync(methodInfo, target, parameters)
            : Task.FromResult(InvokeWithActivitySync(methodInfo, target, parameters));

    public static Task<object?> InvokeWithActivityAsync(this MethodInfo methodInfo, object? target, object?[] parameters) =>
        methodInfo.ReturnsTask()
            ? ((Task)methodInfo.InvokeWithActivity(target, parameters)!).GetReturnValueAsync()
            : Task.FromResult(methodInfo.InvokeWithActivity(target, parameters));

    public static object? InvokeWithActivitySync(this MethodInfo methodInfo, object? target, object?[] parameters) =>
        methodInfo.ReturnsTask()
            ? AsyncHelper.RunSynchronously(
                () =>
                {
                    Task result = (Task)methodInfo.InvokeWithActivity(target, parameters)!;
                    return result.GetReturnValueAsync();
                })
            : methodInfo.InvokeWithActivity(target, parameters);

    public static Task InvokeWithActivityWithoutBlockingAsync(this MethodInfo methodInfo, object? target, object?[] parameters) =>
        methodInfo.ReturnsTask()
            ? Task.Run(() => (Task)methodInfo.InvokeWithActivity(target, parameters)!)
            : Task.Run(() => methodInfo.InvokeWithActivity(target, parameters));

    private static object? InvokeWithActivity(this MethodInfo methodInfo, object? target, object?[] parameters)
    {
        using Activity? activity = ActivitySources.StartInvokeSubscriberActivity(methodInfo);
        return methodInfo.Invoke(target, parameters);
    }
}
