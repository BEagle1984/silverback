using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
