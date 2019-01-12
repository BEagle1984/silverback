// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class AsyncHelper
    {
        public static void RunSynchronously(Func<Task> asyncMethod)
            => Task.Run(asyncMethod).Wait();

        public static TResult RunSynchronously<TResult>(Func<Task<TResult>> asyncMethod)
            => Task.Run(asyncMethod).Result;
    }
}
