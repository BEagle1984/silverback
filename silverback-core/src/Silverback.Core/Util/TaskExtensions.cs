// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Util
{
    public static class TaskExtensions
    {
        public static async Task<object> GetReturnValue(this Task task)
        {
            if (task is Task<object> taskWithReturnValue)
                return await taskWithReturnValue;

            await task;
            return null;
        }
    }
}
