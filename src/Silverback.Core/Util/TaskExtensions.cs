// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Util
{
    public static class TaskExtensions
    {
        public static async Task<object> GetReturnValue(this Task task)
        {
            await task;

            var resultProperty = task.GetType().GetProperty("Result");

            return resultProperty?.GetValue(task);
        }
    }
}