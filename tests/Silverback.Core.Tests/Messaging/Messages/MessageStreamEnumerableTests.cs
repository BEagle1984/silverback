// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages
{
    public class MessageStreamEnumerableTests
    {
        [Fact]
        public async Task PushAsyncAndEnumerate_SomeMessages_MessagesPushedAndReceived()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            using var enumerator = stream.GetEnumerator();

            enumerator.MoveNext();
            enumerator.Current.Should().Be(1);
            enumerator.MoveNext();
            enumerator.Current.Should().Be(2);
            enumerator.MoveNext();
            enumerator.Current.Should().Be(3);
        }

        [Fact]
        public async Task PushAsyncAndEnumerateAsync_SomeMessages_MessagesPushedAndReceived()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            var enumerator = stream.GetAsyncEnumerator();

            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(1);
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(2);
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(3);
        }

        [Fact]
        public async Task PushAsyncCompleteAndEnumerate_SomeMessages_EnumerationCompleted()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            stream.Complete();

            stream.ToList().Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public async Task PushAsyncCompleteAndEnumerateAsync_SomeMessages_EnumerationCompleted()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            stream.Complete();

            (await stream.ToListAsync()).Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public async Task PushAsync_WithBackpressure_BackpressureIsHandled()
        {
            var stream = new MessageStreamEnumerable<int>(2);

            await stream.PushAsync(1, new CancellationTokenSource(100).Token);
            await stream.PushAsync(2, new CancellationTokenSource(100).Token);

            Func<Task> act = async () => await stream.PushAsync(3, new CancellationTokenSource(100).Token);
            act.Should().Throw<OperationCanceledException>();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().Be(1);

            await stream.PushAsync(4, new CancellationTokenSource(100).Token);

            act = async () => await stream.PushAsync(5, new CancellationTokenSource(100).Token);
            act.Should().Throw<OperationCanceledException>();

            stream.Complete();

            (await stream.ToListAsync()).Should().BeEquivalentTo(2, 4);
        }

        [Fact]
        public async Task Complete_TryPushingAfterComplete_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);

            stream.Complete();

            Func<Task> act = async () => await stream.PushAsync(2, new CancellationTokenSource(100).Token);
            act.Should().Throw<InvalidOperationException>();

            (await stream.ToListAsync()).Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task Dispose_TryPushingAfterComplete_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);

            stream.Dispose();

            Func<Task> act = async () => await stream.PushAsync(2, new CancellationTokenSource(100).Token);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task GetLinkedStream_PushingToSourceStream_RelayedToMatchingLinkedStreams()
        {
            var sourceStream = new MessageStreamEnumerable<IMessage>(5);
            var eventsLinkedStream = sourceStream.GetLinkedStream<IEvent>();
            var testEventOnesLinkedStream = sourceStream.GetLinkedStream<TestEventOne>();

            await sourceStream.PushAsync(new TestEventOne());
            await sourceStream.PushAsync(new TestEventTwo());
            await sourceStream.PushAsync(new TestCommandOne());

            sourceStream.Complete(); // Implicitly tests that the Complete call is also propagated

            sourceStream.ToList().Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo(), new TestCommandOne());
            eventsLinkedStream.ToList().Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesLinkedStream.ToList().Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public void GetLinkedStream_CalledTwiceForSameType_SameInstanceReturned()
        {
            var sourceStream = new MessageStreamEnumerable<IMessage>(5);

            var linkedStream1 = sourceStream.GetLinkedStream<IEvent>();
            var linkedStream2 = sourceStream.GetLinkedStream<IEvent>();

            linkedStream1.Should().BeSameAs(linkedStream2);
        }
    }
}
