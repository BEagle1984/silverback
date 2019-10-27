// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class ReflectionHelperTests
    {
        [Fact]
        public void IsAsync_ReturnsTrue_IfAsyncWithTask()
        {
            var methodInfo = GetType().GetMethod(nameof(AsyncWithTask), BindingFlags.Static | BindingFlags.NonPublic);
            methodInfo.ReturnsTask().Should().BeTrue();
        }

        [Fact]
        public void IsAsync_ReturnsFalse_IfAsyncWithoutTask()
        {
            var methodInfo = GetType().GetMethod(nameof(AsyncWithoutTask), BindingFlags.Static | BindingFlags.NonPublic);
            methodInfo.ReturnsTask().Should().BeFalse();
        }

        [Fact]
        public void IsAsync_ReturnsFalse_IfNotAsyncWithVoid()
        {
            var methodInfo = GetType().GetMethod(nameof(NotAsyncWithVoid), BindingFlags.Static | BindingFlags.NonPublic);
            methodInfo.ReturnsTask().Should().BeFalse();
        }

        [Fact]
        public void ReturnsTask_ReturnsTrue_IfNotAsyncWithTask()
        {
            var methodInfo = GetType().GetMethod(nameof(NotAsyncWithTask), BindingFlags.Static | BindingFlags.NonPublic);
            methodInfo.ReturnsTask().Should().BeTrue();
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private static async Task AsyncWithTask() { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private static async void AsyncWithoutTask() { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private static void NotAsyncWithVoid() { }

        private static Task NotAsyncWithTask() => Task.CompletedTask;
    }
}
