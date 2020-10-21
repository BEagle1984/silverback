// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests
{
    // TODO: Change to TimeSpan timeout
    public static class AsyncTestingUtil
    {
        public static void Wait(Func<bool> breakCondition, int timeoutInMilliseconds = 2000)
        {
            const int sleep = 10;
            for (int i = 0; i < timeoutInMilliseconds; i += sleep)
            {
                if (breakCondition())
                    break;

                Thread.Sleep(10);
            }
        }

        public static async Task WaitAsync(Func<bool> breakCondition, int timeoutInMilliseconds = 2000)
        {
            const int sleep = 100;
            for (int i = 0; i < timeoutInMilliseconds; i = i + sleep)
            {
                if (breakCondition())
                    break;

                await Task.Delay(sleep);
            }
        }

        public static async Task WaitAsync(Func<Task<bool>> breakCondition, int timeoutInMilliseconds = 2000)
        {
            const int sleep = 100;
            for (int i = 0; i < timeoutInMilliseconds; i = i + sleep)
            {
                if (await breakCondition())
                    break;

                await Task.Delay(sleep);
            }
        }
    }
}
