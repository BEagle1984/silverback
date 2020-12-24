// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
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
