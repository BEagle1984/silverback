// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages
{
    public class MessageStreamEnumerableTests
    {
        [Fact]
        public async Task PushAsyncAndGetEnumerator_SomeMessages_MessagesPushedAndReceived()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));
            await stream.PushAsync(new PushedMessage(3, 3));

            using var enumerator = stream.GetEnumerator();

            enumerator.MoveNext();
            enumerator.Current.Should().Be(1);
            enumerator.MoveNext();
            enumerator.Current.Should().Be(2);
            enumerator.MoveNext();
            enumerator.Current.Should().Be(3);
        }

        [Fact]
        public async Task PushAsyncAndGetAsyncEnumerator_SomeMessages_MessagesPushedAndReceived()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));
            await stream.PushAsync(new PushedMessage(3, 3));

            var enumerator = stream.GetAsyncEnumerator();

            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(1);
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(2);
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(3);
        }

        [Fact]
        public async Task PushAsyncCompleteAndGetEnumerator_SomeMessages_EnumerationCompleted()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));
            await stream.PushAsync(new PushedMessage(3, 3));

            stream.Complete();

            stream.ToList().Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public async Task PushAsyncCompleteAndGetAsyncEnumerator_SomeMessages_EnumerationCompleted()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));
            await stream.PushAsync(new PushedMessage(3, 3));

            stream.Complete();

            (await stream.ToListAsync()).Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public async Task PushAsync_WhileEnumerating_BackpressureIsHandled()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 2);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));

            var pushTask = stream.PushAsync(new PushedMessage(3, 3));
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().Be(1);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            pushTask = stream.PushAsync(new PushedMessage(4, 4));
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            enumerator.MoveNext();
            enumerator.Current.Should().Be(2);
            enumerator.MoveNext();
            enumerator.Current.Should().Be(3);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            stream.Complete();

            enumerator.MoveNext();
            enumerator.Current.Should().Be(4);

            var completed = !enumerator.MoveNext();
            completed.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileAsyncEnumerating_BackpressureIsHandled()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 2);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));

            var pushTask = stream.PushAsync(new PushedMessage(3, 3));
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(1);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            pushTask = stream.PushAsync(new PushedMessage(4, 4));
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(2);
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(3);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            stream.Complete();

            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(4);

            var completed = !await enumerator.MoveNextAsync();
            completed.Should().BeTrue();
        }

        [Fact]
        public void GetEnumerator_CalledMultipleTimes_OnlyOneConcurrentEnumeratorAllowed()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>());

            using var enumerator = stream.GetEnumerator();

            Action getOtherSyncEnumerator = () =>
            {
                using var enumerator2 = stream.GetEnumerator();
            };
            Action getOtherAsyncEnumerator = () => stream.GetAsyncEnumerator();

            getOtherSyncEnumerator.Should().Throw<InvalidOperationException>();
            getOtherAsyncEnumerator.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetAsyncEnumerator_CalledMultipleTimes_OnlyOneConcurrentEnumeratorAllowed()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>());

            // ReSharper disable once UnusedVariable, needed to avoid CS4014
            var enumerator = stream.GetAsyncEnumerator();

            Action getOtherSyncEnumerator = () =>
            {
                using var enumerator2 = stream.GetEnumerator();
            };
            Action getOtherAsyncEnumerator = () => { stream.GetAsyncEnumerator(); };

            getOtherSyncEnumerator.Should().Throw<InvalidOperationException>();
            getOtherAsyncEnumerator.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task Complete_WhileEnumerating_EnumerationCompleted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));
            await stream.PushAsync(new PushedMessage(3, 3));

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.MoveNext();

            // The next MoveNext reaches the end of the enumerable
            // ReSharper disable once AccessToDisposedClosure
            Task.Run(
                () =>
                {
                    enumerator.MoveNext();
                    completed = true;
                }).RunWithoutBlocking();

            completed.Should().BeFalse();

            stream.Complete();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => completed);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task Complete_WhileAsyncEnumerating_EnumerationCompleted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));
            await stream.PushAsync(new PushedMessage(3, 3));

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // The next MoveNext reaches the end of the enumerable
            Task.Run(
                async () =>
                {
                    await enumerator.MoveNextAsync();
                    completed = true;
                }).RunWithoutBlocking();

            completed.Should().BeFalse();

            stream.Complete();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => completed);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task Complete_TryPushingAfterComplete_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));

            stream.Complete();

            Func<Task> act = async () => await stream.PushAsync(new PushedMessage(3, 3));
            act.Should().Throw<InvalidOperationException>();

            (await stream.ToListAsync()).Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task Dispose_TryPushingAfterDispose_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            await stream.PushAsync(new PushedMessage(1, 1));
            await stream.PushAsync(new PushedMessage(2, 2));

            stream.Dispose();

            Func<Task> act = async () => await stream.PushAsync(new PushedMessage(3, 3));
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Complete_TryPushingFirstMessageAfterComplete_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            stream.Complete();

            Func<Task> act = async () => await stream.PushAsync(new PushedMessage(1, 1));
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Dispose_TryPushingFirstMessageAfterDispose_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(new MessageStreamProvider<object>(), 5);

            stream.Dispose();

            Func<Task> act = async () => await stream.PushAsync(new PushedMessage(1, 1));
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
