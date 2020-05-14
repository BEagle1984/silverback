// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class TaskExtensions
    {
        /// <summary>
        ///     Awaits the specified <see cref="Task" /> and returns a <see cref="Task{T}" /> that wrap either
        ///     the task result or null.
        /// </summary>
        /// <param name="task"> The <see cref="Task" /> to be awaited. </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains either
        ///     the result of the awaited task or <c> null </c>.
        /// </returns>
        public static async Task<object?> GetReturnValue(this Task task)
        {
            await task;

            var resultProperty = task.GetType().GetProperty("Result");

            return resultProperty?.GetValue(task);
        }
    }
}
