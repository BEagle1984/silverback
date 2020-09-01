// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Tests
{
    public static class TaskExtensions
    {
        public static Task RunWithTimeout(this ValueTask task, int timeoutInMilliseconds = 2000) =>
            RunWithTimeout(task.AsTask(), timeoutInMilliseconds);

        public static Task RunWithTimeout(this Task task, int timeoutInMilliseconds = 2000) =>
            Task.WhenAny(task, Task.Delay(timeoutInMilliseconds));

        public static Task RunWithTimeout<T>(this ValueTask<T> task, int timeoutInMilliseconds = 2000) =>
            RunWithTimeout(task.AsTask(), timeoutInMilliseconds);

        public static Task RunWithTimeout<T>(this Task<T> task, int timeoutInMilliseconds = 2000) =>
            Task.WhenAny(task, Task.Delay(timeoutInMilliseconds));

        public static void RunWithoutBlocking(this ValueTask task)
        {
            // This method is used just to trick the compiler and avoid CS4014
        }

        public static void RunWithoutBlocking(this Task task)
        {
            // This method is used just to trick the compiler and avoid CS4014
        }

        public static void RunWithoutBlocking<T>(this ValueTask<T> task)
        {
            // This method is used just to trick the compiler and avoid CS4014
        }

        public static void RunWithoutBlocking<T>(this Task<T> task)
        {
            // This method is used just to trick the compiler and avoid CS4014
        }
    }
}
