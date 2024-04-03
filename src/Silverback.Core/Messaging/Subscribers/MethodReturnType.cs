// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     Describes the sync or async method return type.
/// </summary>
[SuppressMessage("ReSharper", "ConvertToPrimaryConstructor", Justification = "Summary texts")]
public record MethodReturnType
{
    private MethodReturnType()
    {
    }

    /// <summary>
    ///     Gets a value indicating whether the method is returning a <see cref="Task{TResult}" /> or a <see cref="ValueTask{TResult}" />
    ///     with a result.
    /// </summary>
    public bool HasResult => ResultPropertyInfo != null;

    /// <summary>
    ///     Gets a value indicating whether the method is returning a <see cref="System.Threading.Tasks.Task" /> or a
    ///     <see cref="Task{TResult}" />.
    /// </summary>
    public bool IsTask { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the method is returning a <see cref="System.Threading.Tasks.ValueTask" /> or a
    ///     <see cref="ValueTask{TResult}" />.
    /// </summary>
    public bool IsValueTask { get; init; }

    /// <summary>
    ///     Gets the <see cref="PropertyInfo" /> of the <c>Result</c> property of the <see cref="Task{TResult}" /> or
    ///     <see cref="ValueTask{TResult}" />.
    /// </summary>
    private PropertyInfo? ResultPropertyInfo { get; init; }

    private MethodInfo? AsTaskMethodInfo { get; set; }

    /// <summary>
    ///     Creates a <see cref="MethodReturnType" /> with the information from the specified <see cref="MethodInfo" />.
    /// </summary>
    /// <param name="methodInfo">
    ///     The <see cref="MethodInfo" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MethodReturnType" />.
    /// </returns>
    public static MethodReturnType CreateFromMethodInfo(MethodInfo methodInfo)
    {
        Check.NotNull(methodInfo, nameof(methodInfo));

        if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return new MethodReturnType
            {
                IsTask = true,
                ResultPropertyInfo = methodInfo.ReturnType.GetProperty("Result")
            };
        }

        if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            return new MethodReturnType { IsTask = true };

        if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            return new MethodReturnType
            {
                IsValueTask = true,
                AsTaskMethodInfo = methodInfo.ReturnType.GetMethod("AsTask"),
                ResultPropertyInfo = typeof(Task<>).MakeGenericType(methodInfo.ReturnType.GetGenericArguments()[0]).GetProperty("Result")
            };
        }

        if (typeof(ValueTask).IsAssignableFrom(methodInfo.ReturnType))
            return new MethodReturnType { IsValueTask = true };

        return new MethodReturnType();
    }

    /// <summary>
    ///     Handles the result of the method invoked via reflection awaiting the <see cref="Task" /> or <see cref="ValueTask" /> and
    ///     unwrapping the <see cref="Task{TResult}" /> or <see cref="ValueTask{TResult}" /> result.
    /// </summary>
    /// <param name="invokeResult">
    ///     The return value of the call to <see cref="MethodBase.Invoke(object,object[])" />.
    /// </param>
    /// <returns>
    ///     The unwrapped result, or <c>null</c> if the method doesn't have a return value.
    /// </returns>
    public object? AwaitAndUnwrapResult(object? invokeResult) =>
        (IsTask || IsValueTask) && invokeResult != null
            ? AwaitAndUnwrapResultAsync(invokeResult).SafeWait()
            : invokeResult;

    /// <summary>
    ///     Handles the result of the method invoked via reflection awaiting the <see cref="Task" /> or <see cref="ValueTask" /> and
    ///     unwrapping the <see cref="Task{TResult}" /> or <see cref="ValueTask{TResult}" /> result.
    /// </summary>
    /// <param name="invokeResult">
    ///     The return value of the call to <see cref="MethodBase.Invoke(object,object[])" />.
    /// </param>
    /// <returns>
    ///     The unwrapped result, or <c>null</c> if the method doesn't have a return value.
    /// </returns>
    public Task<object?> AwaitAndUnwrapResultAsync(object? invokeResult)
    {
        if (invokeResult == null)
            return Task.FromResult<object?>(null);

        Task? task = null;
        PropertyInfo? resultPropertyInfo = null;

        if (invokeResult is ValueTask valueTask)
        {
            task = valueTask.AsTask();
            resultPropertyInfo = ResultPropertyInfo;
        }
        else if (IsValueTask && AsTaskMethodInfo != null)
        {
            task = (Task?)AsTaskMethodInfo.Invoke(invokeResult, null);
            resultPropertyInfo = ResultPropertyInfo;
        }
        else if (invokeResult is Task resultAsTask)
        {
            task = resultAsTask;
            resultPropertyInfo = ResultPropertyInfo;
        }

        return task != null ? AwaitAndUnwrapResultCoreAsync(task, resultPropertyInfo) : Task.FromResult<object?>(invokeResult);
    }

    private static async Task<object?> AwaitAndUnwrapResultCoreAsync(Task task, PropertyInfo? resultPropertyInfo)
    {
        await task.ConfigureAwait(false);
        return resultPropertyInfo?.GetValue(task);
    }
}
