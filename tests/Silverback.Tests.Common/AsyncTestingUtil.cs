// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Tests
{
    public static class AsyncTestingUtil
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);

        public static Task WaitAsync(Func<bool> breakCondition, TimeSpan? timeout = null) =>
            WaitAsync(() => Task.FromResult(breakCondition()), timeout);

        public static async Task WaitAsync(Func<Task<bool>> breakCondition, TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(2);

            for (double i = 0; i < timeout.Value.TotalMilliseconds; i += Interval.TotalMilliseconds)
            {
                try
                {
                    if (await breakCondition())
                        break;
                }
                catch (Exception)
                {
                    // Ignore
                }

                await Task.Delay(Interval);
            }
        }
    }
}
