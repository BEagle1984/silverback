// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages
{
    public class MessageStreamProviderTests
    {
        [Fact]
        public async Task PushAsync_Messages_RelayedToMatchingStreams()
        {
            var provider = new MessageStreamProvider<IMessage>();

            List<IEvent>? eventsList = null;
            List<TestEventOne>? testEventOnesList = null;

            var eventsStream = provider.CreateStream<IEvent>();
            var testEventOneStream = provider.CreateStream<TestEventOne>();

            var task1 = Task.Run(() => eventsList = eventsStream.ToList());
            var task2 = Task.Run(() => testEventOnesList = testEventOneStream.ToList());

            await provider.PushAsync(new TestEventOne());
            await provider.PushAsync(new TestEventTwo());
            await provider.PushAsync(new TestCommandOne());

            await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

            await Task.WhenAll(task1, task2);

            eventsList.Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesList.Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public async Task PushAsync_Envelopes_RelayedToStream()
        {
            var provider = new MessageStreamProvider<TestEnvelope>();
            var stream = provider.CreateStream<TestEnvelope>();

            List<TestEnvelope>? envelopes = null;

            var task = Task.Run(() => envelopes = stream.ToList());

            await provider.PushAsync(new TestEnvelope(new TestEventOne()));
            await provider.PushAsync(new TestEnvelope(new TestEventTwo()));
            await provider.PushAsync(new TestEnvelope(new TestCommandOne()));

            await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

            await task;

            envelopes.Should().HaveCount(3);
        }

        [Fact]
        public async Task PushAsync_Envelopes_UnwrappedAndRelayedToMatchingStreams()
        {
            var provider = new MessageStreamProvider<TestEnvelope>();
            var eventsStream = provider.CreateStream<IEvent>();
            var testEventOnesStream = provider.CreateStream<TestEventOne>();

            List<IEvent>? eventsList = null;
            List<TestEventOne>? testEventOnesList = null;

            var task1 = Task.Run(() => eventsList = eventsStream.ToList());
            var task2 = Task.Run(() => testEventOnesList = testEventOnesStream.ToList());

            await provider.PushAsync(new TestEnvelope(new TestEventOne()));
            await provider.PushAsync(new TestEnvelope(new TestEventTwo()));
            await provider.PushAsync(new TestEnvelope(new TestCommandOne()));

            await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

            await Task.WhenAll(task1, task2);

            eventsList.Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesList.Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public async Task PushAsync_WhileEnumeratingStream_BackpressureIsHandled()
        {
            var provider = new MessageStreamProvider<int>();
            var stream = provider.CreateStream<int>();
            using var enumerator = stream.GetEnumerator();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

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
        public async Task PushAsync_WhileAsyncEnumeratingStream_BackpressureIsHandled()
        {
            var provider = new MessageStreamProvider<int>();
            var stream = provider.CreateStream<int>();
            var enumerator = stream.GetAsyncEnumerator();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

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
        public async Task PushAsync_WhileEnumeratingMultipleStreams_BackpressureIsHandled()
        {
            var provider = new MessageStreamProvider<int>();
            var stream1 = provider.CreateStream<int>();
            var stream2 = provider.CreateStream<int>();
            using var enumerator1 = stream1.GetEnumerator();
            using var enumerator2 = stream2.GetEnumerator();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            enumerator1.MoveNext();
            enumerator2.MoveNext();

            await Task.Delay(100);
            pushTask1.IsCompleted.Should().BeFalse();

            enumerator1.MoveNext();

            await Task.Delay(100);
            pushTask1.IsCompleted.Should().BeFalse();

            enumerator2.MoveNext();

            await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
            pushTask1.IsCompleted.Should().BeTrue();

            await Task.Delay(100);
            pushTask2.IsCompleted.Should().BeFalse();

            enumerator1.MoveNext();

            await Task.Delay(100);
            pushTask2.IsCompleted.Should().BeFalse();

            enumerator2.MoveNext();

            await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
            pushTask2.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileAsyncEnumeratingMultipleStreams_BackpressureIsHandled()
        {
            var provider = new MessageStreamProvider<int>();
            var stream1 = provider.CreateStream<int>();
            var stream2 = provider.CreateStream<int>();
            var enumerator1 = stream1.GetAsyncEnumerator();
            var enumerator2 = stream2.GetAsyncEnumerator();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            await enumerator1.MoveNextAsync();
            await enumerator2.MoveNextAsync();

            await Task.Delay(100);
            pushTask1.IsCompleted.Should().BeFalse();

            await enumerator1.MoveNextAsync();

            await Task.Delay(100);
            pushTask1.IsCompleted.Should().BeFalse();

            await enumerator2.MoveNextAsync();

            await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
            pushTask1.IsCompleted.Should().BeTrue();

            await Task.Delay(100);
            pushTask2.IsCompleted.Should().BeFalse();

            await enumerator1.MoveNextAsync();

            await Task.Delay(100);
            pushTask2.IsCompleted.Should().BeFalse();

            await enumerator2.MoveNextAsync();

            await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
            pushTask2.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WithSingleStream_ProcessedCallbackInvokedWhenProcessedByStream()
        {
            var processed = new List<int>();
            var provider = new MessageStreamProvider<int>
            {
                ProcessedCallback = message =>
                {
                    processed.Add(message);
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            processed.Should().BeEmpty();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();

            // One extra MoveNext is needed to invoke the callback for the previous message
            enumerator.MoveNext();

            await Task.WhenAll(pushTask1, pushTask2);

            processed.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task PushAsync_WithStreams_ProcessedCallbackInvokedWhenProcessedByAllStreams()
        {
            var processed = new List<int>();
            var provider = new MessageStreamProvider<int>
            {
                ProcessedCallback = message =>
                {
                    processed.Add(message);
                    return Task.CompletedTask;
                }
            };

            var stream1 = provider.CreateStream<int>();
            var stream2 = provider.CreateStream<int>();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            processed.Should().BeEmpty();

            using var enumerator1 = stream1.GetEnumerator();
            enumerator1.MoveNext();
            enumerator1.MoveNext();
            enumerator1.MoveNext();

            var enumerator2 = stream2.GetAsyncEnumerator();
            await enumerator2.MoveNextAsync();
            await enumerator2.MoveNextAsync();

            await pushTask1;

            processed.Should().BeEquivalentTo(1);

            await enumerator2.MoveNextAsync();

            await pushTask2;

            processed.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task PushAsync_WithNoStreams_ProcessedCallbackInvokedImmediately()
        {
            var processed = new List<object>();
            var provider = new MessageStreamProvider<object>
            {
                ProcessedCallback = message =>
                {
                    processed.Add(message);
                    return Task.CompletedTask;
                }
            };

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            processed.Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public async Task PushAsync_WithNotMatchingStreams_ProcessedCallbackInvokedImmediately()
        {
            var processed = new List<object>();
            var provider = new MessageStreamProvider<object>
            {
                ProcessedCallback = message =>
                {
                    processed.Add(message);
                    return Task.CompletedTask;
                }
            };

            provider.CreateStream<string>();
            provider.CreateStream<bool>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            processed.Should().BeEquivalentTo(1, 2, 3);
        }

        [Fact]
        public async Task CompleteAsync_CompletedAndPulledAll_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            completed.Should().BeFalse();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.MoveNext();

            completed.Should().BeFalse();

            await Task.WhenAll(pushTask1, pushTask2);

            var completeTask = provider.CompleteAsync();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNext();

            await Task.WhenAll(pushTask3, completeTask);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_PulledAllAndCompleted_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            completed.Should().BeFalse();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.MoveNext();

            // The next MoveNext reaches the end of the enumerable
            // ReSharper disable once AccessToDisposedClosure
            Task.Run(() => enumerator.MoveNext()).RunWithoutBlocking();

            completed.Should().BeFalse();

            await Task.WhenAll(pushTask1, pushTask2);

            await provider.CompleteAsync();

            // Give the other thread a chance to exit the MoveNext and invoke the callback
            await Task.Delay(100);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_CompletedAndEnumerated_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            completed.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            await Task.WhenAll(pushTask1, pushTask2);

            completed.Should().BeFalse();

            var completeTask = provider.CompleteAsync();

            // The next MoveNext reaches the end of the enumerable
            await enumerator.MoveNextAsync();

            await Task.WhenAll(pushTask3, completeTask);

            await AsyncTestingUtil.WaitAsync(() => completed);
            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_EnumeratedAndCompleted_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            completed.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            await Task.WhenAll(pushTask1, pushTask2);

            completed.Should().BeFalse();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNextAsync().RunWithoutBlocking();

            var completeTask = provider.CompleteAsync();

            await Task.WhenAll(pushTask3, completeTask);

            await AsyncTestingUtil.WaitAsync(() => completed);
            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_PushCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>
            {
                PushCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            completed.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNextAsync().RunWithoutBlocking();

            await Task.WhenAll(pushTask1, pushTask2);

            completed.Should().BeFalse();

            await provider.CompleteAsync();

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_WhileEnumeratingStream_EnumerationCompleted()
        {
            int? count = null;
            var provider = new MessageStreamProvider<int>();
            var stream = provider.CreateStream<int>();

            var enumerationTask = Task.Run(() => { count = stream.Count(); });

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            count.Should().BeNull();

            await provider.CompleteAsync();
            await enumerationTask;

            count.Should().Be(3);
        }

        [Fact]
        public async Task CompleteAsync_WhileAsyncEnumeratingStreams_EnumerationCompleted()
        {
            int? count = null;
            var provider = new MessageStreamProvider<int>();
            var stream = provider.CreateStream<int>();

            var enumerationTask = Task.Run(async () => { count = await stream.CountAsync(); });

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            count.Should().BeNull();

            await provider.CompleteAsync();
            await enumerationTask;

            count.Should().Be(3);
        }

        [Fact]
        public async Task AbortAsync_PushAbortedCallbackInvoked()
        {
            var aborted = false;
            var provider = new MessageStreamProvider<int>
            {
                PushAbortedCallback = () =>
                {
                    aborted = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            var pushTask1 = provider.PushAsync(1);
            var pushTask2 = provider.PushAsync(2);
            var pushTask3 = provider.PushAsync(3);

            aborted.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNextAsync().RunWithoutBlocking();

            await Task.WhenAll(pushTask1, pushTask2);

            aborted.Should().BeFalse();

            await provider.AbortAsync();

            aborted.Should().BeTrue();
        }

        [Fact]
        public async Task AbortAsync_WhileEnumeratingStream_EnumerationAborted()
        {
            int? count = null;
            var provider = new MessageStreamProvider<int>();
            var stream = provider.CreateStream<int>();

            var enumerationTask = Task.Run(() => { count = stream.Count(); });

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            count.Should().BeNull();

            await provider.AbortAsync();

            await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

            count.Should().BeNull();
            enumerationTask.Status.Should().Be(TaskStatus.Faulted);
        }

        [Fact]
        public async Task AbortAsync_WhileAsyncEnumeratingStreams_EnumerationAborted()
        {
            int? count = null;
            var provider = new MessageStreamProvider<int>();
            var stream = provider.CreateStream<int>();

            var enumerationTask = Task.Run(async () => { count = await stream.CountAsync(); });

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            count.Should().BeNull();

            await provider.AbortAsync();

            await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

            count.Should().BeNull();
            enumerationTask.Status.Should().Be(TaskStatus.Canceled);
        }

        [Fact]
        public void CreateStream_CalledTwiceForSameType_NewInstanceReturned()
        {
            var provider = new MessageStreamProvider<IMessage>();
            var stream1 = provider.CreateStream<IEvent>();
            var stream2 = provider.CreateStream<IEvent>();

            stream2.Should().NotBeSameAs(stream1);
        }

        [Fact]
        public void CreateStream_GenericAndNonGenericVersions_EquivalentInstanceReturned()
        {
            var provider = new MessageStreamProvider<IMessage>();

            var stream1 = provider.CreateStream<IEvent>();
            var stream2 = provider.CreateStream(typeof(IEvent));

            stream2.Should().BeOfType(stream1.GetType());
        }
    }
}
