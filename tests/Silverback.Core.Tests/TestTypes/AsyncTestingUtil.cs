using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests.Core.TestTypes
{
    public static class AsyncTestingUtil
    {
        public static void Wait(Func<bool> breakCondition, int timeoutInMilliseconds = 1000)
        {
            const int sleep = 10;
            for (int i = 0; i < timeoutInMilliseconds; i = i + sleep)
            {
                if (breakCondition())
                    break;

                Thread.Sleep(10);
            }
        }

        public static async Task WaitAsync(Func<bool> breakCondition, int timeoutInMilliseconds = 1000)
        {
            const int sleep = 10;
            for (int i = 0; i < timeoutInMilliseconds; i = i + sleep)
            {
                if (breakCondition())
                    break;

                await Task.Delay(sleep);
            }
        }
    }
}
