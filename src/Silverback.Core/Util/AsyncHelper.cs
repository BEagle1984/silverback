// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

/*  Original file: https://github.com/aspnet/AspNetIdentity/blob/master/src/Microsoft.AspNet.Identity.Core/AsyncHelper.cs
    Copyright(c) Microsoft Corporation
    All rights reserved.
    MIT License
    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the Software), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class AsyncHelper
    {
        private static readonly TaskFactory TaskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        public static TResult RunSynchronously<TResult>(Func<Task<TResult>> func)
        {
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;

            Task<TResult> ExecuteTask()
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = uiCulture;
                return func();
            }

            return TaskFactory.StartNew(
                    ExecuteTask,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default)
                .Unwrap()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public static void RunSynchronously(Func<Task> func)
        {
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;

            Task ExecuteTask()
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = uiCulture;
                return func();
            }

            TaskFactory.StartNew(
                    ExecuteTask,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default)
                .Unwrap()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
