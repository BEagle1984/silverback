// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Core.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages
{
    public class MessageStreamEnumerableTests
    {
        [Fact]
        public async Task PushAsyncAndGetEnumerator_SomeMessages_MessagesPushedAndReceived()
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
        public async Task PushAsyncAndGetAsyncEnumerator_SomeMessages_MessagesPushedAndReceived()
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
        public async Task PushAsyncCompleteAndGetEnumerator_SomeMessages_EnumerationCompleted()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            stream.Complete();

            stream.ToList().Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public async Task PushAsyncCompleteAndGetAsyncEnumerator_SomeMessages_EnumerationCompleted()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            stream.Complete();

            (await stream.ToListAsync()).Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public async Task PushAsync_WhileEnumerating_BackpressureIsHandled()
        {
            var stream = new MessageStreamEnumerable<int>(2);

            await stream.PushAsync(1, new CancellationTokenSource(2000).Token);
            await stream.PushAsync(2, new CancellationTokenSource(2000).Token);

            var pushTask = stream.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().Be(1);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            pushTask = stream.PushAsync(4);
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
            var stream = new MessageStreamEnumerable<int>(2);

            await stream.PushAsync(1, new CancellationTokenSource(2000).Token);
            await stream.PushAsync(2, new CancellationTokenSource(2000).Token);

            var pushTask = stream.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(1);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            pushTask = stream.PushAsync(4);
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
        public async Task PushAsync_WhileEnumeratingLinkedStream_BackpressureIsHandled()
        {
            var sourceStream = new MessageStreamEnumerable<int>(2);
            var linkedStream = sourceStream.CreateLinkedStream<int>();

            await sourceStream.PushAsync(1, new CancellationTokenSource(2000).Token);
            await sourceStream.PushAsync(2, new CancellationTokenSource(2000).Token);

            var pushTask = sourceStream.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            using var enumerator = linkedStream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().Be(1);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            pushTask = sourceStream.PushAsync(4);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            enumerator.MoveNext();
            enumerator.Current.Should().Be(2);
            enumerator.MoveNext();
            enumerator.Current.Should().Be(3);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            sourceStream.Complete();

            enumerator.MoveNext();
            enumerator.Current.Should().Be(4);

            var completed = !enumerator.MoveNext();
            completed.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileAsyncEnumeratingLinkedStream_BackpressureIsHandled()
        {
            var sourceStream = new MessageStreamEnumerable<int>(2);
            var linkedStream = sourceStream.CreateLinkedStream<int>();

            await sourceStream.PushAsync(1, new CancellationTokenSource(2000).Token);
            await sourceStream.PushAsync(2, new CancellationTokenSource(2000).Token);

            var pushTask = sourceStream.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            var enumerator = linkedStream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(1);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            pushTask = sourceStream.PushAsync(4);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(2);
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(3);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            sourceStream.Complete();

            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(4);

            var completed = !await enumerator.MoveNextAsync();
            completed.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileEnumeratingMultipleLinkedStreams_BackpressureIsHandled()
        {
            var sourceStream = new MessageStreamEnumerable<int>(2);
            var linkedStream1 = sourceStream.CreateLinkedStream<int>();
            var linkedStream2 = sourceStream.CreateLinkedStream<int>();

            await sourceStream.PushAsync(1, new CancellationTokenSource(2000).Token);
            await sourceStream.PushAsync(2, new CancellationTokenSource(2000).Token);

            var pushTask = sourceStream.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            using var enumerator1 = linkedStream1.GetEnumerator();
            enumerator1.MoveNext();

            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            using var enumerator2 = linkedStream2.GetEnumerator();
            enumerator2.MoveNext();

            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileAsyncEnumeratingMultipleLinkedStreams_BackpressureIsHandled()
        {
            var sourceStream = new MessageStreamEnumerable<int>(2);
            var linkedStream1 = sourceStream.CreateLinkedStream<int>();
            var linkedStream2 = sourceStream.CreateLinkedStream<int>();

            await sourceStream.PushAsync(1, new CancellationTokenSource(2000).Token);
            await sourceStream.PushAsync(2, new CancellationTokenSource(2000).Token);

            var pushTask = sourceStream.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            var enumerator1 = linkedStream1.GetAsyncEnumerator();
            await enumerator1.MoveNextAsync();

            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            var enumerator2 = linkedStream2.GetAsyncEnumerator();
            await enumerator2.MoveNextAsync();

            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task GetEnumerator_WithSomeMessages_MessageProcessedCallbackInvoked()
        {
            var processed = new List<int>();
            var stream = new MessageStreamEnumerable<int>(5)
            {
                ProcessedCallback = message =>
                {
                    processed.Add(message);
                    return Task.CompletedTask;
                }
            };

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            processed.Should().BeEmpty();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();

            // One extra MoveNext is needed to invoke the callback for the previous message
            enumerator.MoveNext();

            processed.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task GetAsyncEnumerator_WithSomeMessages_MessageProcessedCallbackInvoked()
        {
            var processed = new List<int>();
            var stream = new MessageStreamEnumerable<int>(5)
            {
                ProcessedCallback = message =>
                {
                    processed.Add(message);
                    return Task.CompletedTask;
                }
            };

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            processed.Should().BeEmpty();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // One extra MoveNext is needed to invoke the callback for the previous message
            await enumerator.MoveNextAsync();

            processed.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task GetEnumerator_CompletedAndPulledAllMessages_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(5)
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);
            stream.Complete();

            completed.Should().BeFalse();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.MoveNext();

            completed.Should().BeFalse();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNext();

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task GetEnumerator_PulledAllMessagesAndCompleted_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(5)
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            completed.Should().BeFalse();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.MoveNext();

            // The next MoveNext reaches the end of the enumerable
            // ReSharper disable once AccessToDisposedClosure
            Task.Run(() => enumerator.MoveNext()).RunWithoutBlocking();

            completed.Should().BeFalse();

            stream.Complete();

            // Give the other thread a chance to exit the MoveNext and invoke the callback
            await Task.Delay(100);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task GetAsyncEnumerator_CompletedAndPulledAllMessages_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(5)
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);
            stream.Complete();

            completed.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            completed.Should().BeFalse();

            // The next MoveNext reaches the end of the enumerable
            await enumerator.MoveNextAsync();

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task GetAsyncEnumerator_PulledAllMessagesAndCompleted_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(5)
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            completed.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNextAsync().RunWithoutBlocking();

            completed.Should().BeFalse();

            stream.Complete();

            // Give the other thread a chance to exit the MoveNext and invoke the callback
            await Task.Delay(100);

            completed.Should().BeTrue();
        }

        [Fact]
        public void GetEnumerator_CalledMultipleTimes_OnlyOneConcurrentEnumeratorAllowed()
        {
            var stream = new MessageStreamEnumerable<int>();

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
        public async Task GetAsyncEnumerator_CalledMultipleTimes_OnlyOneConcurrentEnumeratorAllowed()
        {
            var stream = new MessageStreamEnumerable<int>();
            await stream.PushAsync(1);

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
        public async Task Complete_PushCompletedCallbackInvoked()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(5)
            {
                PushCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            completed.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNextAsync().RunWithoutBlocking();

            completed.Should().BeFalse();

            stream.Complete();

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task Complete_WhileEnumerating_EnumerationCompleted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

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
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

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
        public async Task Complete_WhileEnumeratingLinkedStreams_EnumerationCompleted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(5);
            var linkedStream = stream.CreateLinkedStream<int>();

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            using var enumerator = linkedStream.GetEnumerator();
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
        public async Task Complete_WhileAsyncEnumeratingLinkedStreams_EnumerationCompleted()
        {
            var completed = false;
            var stream = new MessageStreamEnumerable<int>(5);
            var linkedStream = stream.CreateLinkedStream<int>();

            await stream.PushAsync(1);
            await stream.PushAsync(2);
            await stream.PushAsync(3);

            var enumerator = linkedStream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // The next MoveNext reaches the end of the enumerable
            // ReSharper disable once AccessToDisposedClosure
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
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);

            stream.Complete();

            Func<Task> act = async () => await stream.PushAsync(2, new CancellationTokenSource(2000).Token);
            act.Should().Throw<InvalidOperationException>();

            (await stream.ToListAsync()).Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task Dispose_TryPushingAfterDispose_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            await stream.PushAsync(1);
            await stream.PushAsync(2);

            stream.Dispose();

            Func<Task> act = async () => await stream.PushAsync(2, new CancellationTokenSource(2000).Token);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Complete_TryPushingFirstMessageAfterComplete_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            stream.Complete();

            Func<Task> act = async () => await stream.PushAsync(2, new CancellationTokenSource(2000).Token);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Dispose_TryPushingFirstMessageAfterDispose_ExceptionThrown()
        {
            var stream = new MessageStreamEnumerable<int>(5);

            stream.Dispose();

            Func<Task> act = async () => await stream.PushAsync(2, new CancellationTokenSource(2000).Token);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateLinkedStream_PushingToSourceStream_RelayedToMatchingLinkedStreams()
        {
            var sourceStream = new MessageStreamEnumerable<IMessage>(5);
            var eventsLinkedStream = sourceStream.CreateLinkedStream<IEvent>();
            var testEventOnesLinkedStream = sourceStream.CreateLinkedStream<TestEventOne>();

            await sourceStream.PushAsync(new TestEventOne());
            await sourceStream.PushAsync(new TestEventTwo());
            await sourceStream.PushAsync(new TestCommandOne());

            sourceStream.Complete(); // Implicitly tests that the Complete call is also propagated

            eventsLinkedStream.ToList().Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesLinkedStream.ToList().Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public async Task CreateLinkedStream_PushingToSourceStream_EnvelopeUnwrappedAndRelayedToMatchingLinkedStreams()
        {
            var sourceStream = new MessageStreamEnumerable<TestEnvelope>(5);
            var eventsLinkedStream = sourceStream.CreateLinkedStream<IEvent>();
            var testEventOnesLinkedStream = sourceStream.CreateLinkedStream<TestEventOne>();

            await sourceStream.PushAsync(new TestEnvelope(new TestEventOne()));
            await sourceStream.PushAsync(new TestEnvelope(new TestEventTwo()));
            await sourceStream.PushAsync(new TestEnvelope(new TestCommandOne()));

            sourceStream.Complete(); // Implicitly tests that the Complete call is also propagated

            eventsLinkedStream.ToList().Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesLinkedStream.ToList().Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public void CreateLinkedStream_CalledTwiceForSameType_NewInstanceReturned()
        {
            var sourceStream = new MessageStreamEnumerable<IMessage>(5);
            var linkedStream1 = sourceStream.CreateLinkedStream<IEvent>();
            var linkedStream2 = sourceStream.CreateLinkedStream<IEvent>();

            linkedStream2.Should().NotBeSameAs(linkedStream1);
        }

        [Fact]
        public void CreateLinkedStream_GenericAndNonGenericVersions_EquivalentInstanceReturned()
        {
            var sourceStream = new MessageStreamEnumerable<IMessage>(5);

            var linkedStream1 = sourceStream.CreateLinkedStream<IEvent>();
            var linkedStream2 = sourceStream.CreateLinkedStream(typeof(IEvent));

            linkedStream2.Should().BeOfType(linkedStream1.GetType());
        }
    }
}
