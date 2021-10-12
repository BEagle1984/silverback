// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages
{
    public class MessageStreamEnumerableTests
    {
        [Fact]
        public async Task PushAsyncGetEnumeratorAndCompleteAsync_SomeMessages_MessagesPushedAndReceived()
        {
            var stream = new MessageStreamEnumerable<int>();
            var success = false;

            var enumerationTask = Task.Run(
                () =>
                {
                    using var enumerator = stream.GetEnumerator();
                    enumerator.MoveNext().Should().BeTrue();
                    enumerator.Current.Should().Be(1);
                    enumerator.MoveNext().Should().BeTrue();
                    enumerator.Current.Should().Be(2);
                    enumerator.MoveNext().Should().BeTrue();
                    enumerator.Current.Should().Be(3);
                    enumerator.MoveNext().Should().BeFalse();
                    success = true;
                });

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));
            await stream.PushAsync(new PushedMessage(3, 3));

            await stream.CompleteAsync();

            await enumerationTask;

            success.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsynGetAsyncEnumeratorAndCompleteAsync_SomeMessages_MessagesPushedAndReceived()
        {
            var stream = new MessageStreamEnumerable<int>();
            var success = false;

            var enumerationTask = Task.Run(
                async () =>
                {
                    var enumerator = stream.GetAsyncEnumerator();
                    (await enumerator.MoveNextAsync()).Should().BeTrue();
                    enumerator.Current.Should().Be(1);
                    (await enumerator.MoveNextAsync()).Should().BeTrue();
                    enumerator.Current.Should().Be(2);
                    (await enumerator.MoveNextAsync()).Should().BeTrue();
                    enumerator.Current.Should().Be(3);
                    (await enumerator.MoveNextAsync()).Should().BeFalse();
                    success = true;
                });

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));
            await stream.PushAsync(new PushedMessage(3, 3));

            await stream.CompleteAsync();

            await enumerationTask;

            success.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileEnumerating_BackpressureIsHandled()
        {
            var stream = new MessageStreamEnumerable<int>();
            using var enumerator = stream.GetEnumerator();

            var pushTask1 = stream.PushAsync(new PushedMessage(1, 1));
            var pushTask2 = stream.PushAsync(new PushedMessage(2, 2));
            var pushTask3 = stream.PushAsync(new PushedMessage(3, 3));

            enumerator.MoveNext();

            await Task.Delay(100);
            pushTask1.IsCompleted.Should().BeFalse();

            enumerator.MoveNext();

            await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
            pushTask1.IsCompleted.Should().BeTrue();

            await Task.Delay(100);
            pushTask2.IsCompleted.Should().BeFalse();
            pushTask3.IsCompleted.Should().BeFalse();

            enumerator.MoveNext();

            await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
            pushTask2.IsCompleted.Should().BeTrue();
            pushTask3.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task PushAsync_WhileAsyncEnumerating_BackpressureIsHandled()
        {
            var stream = new MessageStreamEnumerable<int>();
            var enumerator = stream.GetAsyncEnumerator();

            var pushTask1 = stream.PushAsync(new PushedMessage(1, 1));
            var pushTask2 = stream.PushAsync(new PushedMessage(2, 2));
            var pushTask3 = stream.PushAsync(new PushedMessage(3, 3));

            await enumerator.MoveNextAsync();

            await Task.Delay(100);
            pushTask1.IsCompleted.Should().BeFalse();

            await enumerator.MoveNextAsync();

            await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
            pushTask1.IsCompleted.Should().BeTrue();

            await Task.Delay(100);
            pushTask2.IsCompleted.Should().BeFalse();
            pushTask3.IsCompleted.Should().BeFalse();

            await enumerator.MoveNextAsync();

            await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
            pushTask2.IsCompleted.Should().BeTrue();
            pushTask3.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task CompleteAsync_WhileEnumerating_EnumerationCompleted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>();
            using var enumerator = stream.GetEnumerator();

            // The next MoveNext reaches the end of the enumerable
            // ReSharper disable once AccessToDisposedClosure
            Task.Run(
                () =>
                {
                    enumerator.MoveNext();
                    completed = true;
                }).FireAndForget();

            completed.Should().BeFalse();

            await stream.CompleteAsync();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => completed);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_WhileAsyncEnumerating_EnumerationCompleted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>();
            var enumerator = stream.GetAsyncEnumerator();

            // The next MoveNext reaches the end of the enumerable
            Task.Run(
                async () =>
                {
                    await enumerator.MoveNextAsync();
                    completed = true;
                }).FireAndForget();

            completed.Should().BeFalse();

            await stream.CompleteAsync();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => completed);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task Abort_WhileEnumerating_EnumerationAborted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>();
            using var enumerator = stream.GetEnumerator();

            // ReSharper disable once AccessToDisposedClosure
            var enumerationTask = Task.Run(
                () =>
                {
                    enumerator.MoveNext();
                    completed = true;
                });

            completed.Should().BeFalse();

            stream.Abort();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

            completed.Should().BeFalse();
            enumerationTask.Status.Should().Be(TaskStatus.Faulted);
            enumerationTask.Exception!.InnerExceptions.First().Should().BeAssignableTo<OperationCanceledException>();
        }

        [Fact]
        public async Task Abort_WhileAsyncEnumerating_EnumerationAborted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>();
            var enumerator = stream.GetAsyncEnumerator();

            var enumerationTask = Task.Run(
                async () =>
                {
                    await enumerator.MoveNextAsync();
                    completed = true;
                });

            completed.Should().BeFalse();

            stream.Abort();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

            completed.Should().BeFalse();
            enumerationTask.Status.Should().Be(TaskStatus.Canceled);
        }

        [Fact]
        public async Task Abort_WhilePushing_PushAborted()
        {
            var pushed = false;
            var stream = new MessageStreamEnumerable<int>();

            var pushTask = Task.Run(
                async () =>
                {
                    await stream.PushAsync(new PushedMessage(1, 1));
                    pushed = true;
                });

            pushed.Should().BeFalse();

            stream.Abort();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);

            pushed.Should().BeFalse();
            pushTask.Status.Should().Be(TaskStatus.Canceled);
        }

        [Fact]
        public async Task CompleteAsync_TryPushingAfterComplete_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>();

            await stream.CompleteAsync();

            Func<Task> act = async () => await stream.PushAsync(new PushedMessage(1, 1));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task Dispose_TryPushingAfterDispose_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>();
            stream.Dispose();

            Func<Task> act = async () => await stream.PushAsync(new PushedMessage(1, 1));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
