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
        public async Task PushAsync_WithStreams_RelayedToMatchingStreams()
        {
            var provider = new MessageStreamProvider<IMessage>(5);
            var eventsStream = provider.CreateStream<IEvent>();
            var testEventOnesStream = provider.CreateStream<TestEventOne>();

            await provider.PushAsync(new TestEventOne());
            await provider.PushAsync(new TestEventTwo());
            await provider.PushAsync(new TestCommandOne());

            await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

            eventsStream.ToList().Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesStream.ToList().Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public async Task PushAsync_WithStreams_EnvelopeRelayed()
        {
            var provider = new MessageStreamProvider<TestEnvelope>(5);
            var stream = provider.CreateStream<IEnvelope>();

            await provider.PushAsync(new TestEnvelope(new TestEventOne()));
            await provider.PushAsync(new TestEnvelope(new TestEventTwo()));
            await provider.PushAsync(new TestEnvelope(new TestCommandOne()));

            await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

            stream.Count().Should().Be(3);
        }

        [Fact]
        public async Task PushAsync_WithStreams_EnvelopeUnwrappedAndRelayedToMatchingStreams()
        {
            var provider = new MessageStreamProvider<TestEnvelope>(5);
            var eventsStream = provider.CreateStream<IEvent>();
            var testEventOnesStream = provider.CreateStream<TestEventOne>();

            await provider.PushAsync(new TestEnvelope(new TestEventOne()));
            await provider.PushAsync(new TestEnvelope(new TestEventTwo()));
            await provider.PushAsync(new TestEnvelope(new TestCommandOne()));

            await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

            eventsStream.ToList().Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesStream.ToList().Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public async Task PushAsync_WhileEnumeratingStream_BackpressureIsHandled()
        {
            var provider = new MessageStreamProvider<int>(2);
            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);

            var pushTask = provider.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().Be(1);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            pushTask = provider.PushAsync(4);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            enumerator.MoveNext();
            enumerator.Current.Should().Be(2);
            enumerator.MoveNext();
            enumerator.Current.Should().Be(3);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            await provider.CompleteAsync();

            enumerator.MoveNext();
            enumerator.Current.Should().Be(4);

            var completed = !enumerator.MoveNext();
            completed.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileAsyncEnumeratingStream_BackpressureIsHandled()
        {
            var provider = new MessageStreamProvider<int>(2);
            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);

            var pushTask = provider.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(1);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            pushTask = provider.PushAsync(4);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(2);
            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(3);

            await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
            pushTask.IsCompleted.Should().BeTrue();

            await provider.CompleteAsync();

            await enumerator.MoveNextAsync();
            enumerator.Current.Should().Be(4);

            var completed = !await enumerator.MoveNextAsync();
            completed.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileEnumeratingMultipleStreams_BackpressureIsHandled()
        {
            var provider = new MessageStreamProvider<int>(2);
            var stream1 = provider.CreateStream<int>();
            var stream2 = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);

            var pushTask = provider.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            using var enumerator1 = stream1.GetEnumerator();
            enumerator1.MoveNext();

            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            using var enumerator2 = stream2.GetEnumerator();
            enumerator2.MoveNext();

            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WhileAsyncEnumeratingMultipleStreams_BackpressureIsHandled()
        {
            var provider = new MessageStreamProvider<int>(2);
            var stream1 = provider.CreateStream<int>();
            var stream2 = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);

            var pushTask = provider.PushAsync(3);
            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            var enumerator1 = stream1.GetAsyncEnumerator();
            await enumerator1.MoveNextAsync();

            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeFalse();

            var enumerator2 = stream2.GetAsyncEnumerator();
            await enumerator2.MoveNextAsync();

            await Task.Delay(100);
            pushTask.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task PushAsync_WithSingleStream_ProcessedCallbackInvokedWhenProcessedByStream()
        {
            var processed = new List<int>();
            var provider = new MessageStreamProvider<int>(5)
            {
                ProcessedCallback = message =>
                {
                    processed.Add(message);
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            processed.Should().BeEmpty();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();

            // One extra MoveNext is needed to invoke the callback for the previous message
            enumerator.MoveNext();

            processed.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task PushAsync_WithStreams_ProcessedCallbackInvokedWhenProcessedByAllStreams()
        {
            var processed = new List<int>();
            var provider = new MessageStreamProvider<int>(5)
            {
                ProcessedCallback = message =>
                {
                    processed.Add(message);
                    return Task.CompletedTask;
                }
            };

            var stream1 = provider.CreateStream<int>();
            var stream2 = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            processed.Should().BeEmpty();

            using var enumerator1 = stream1.GetEnumerator();
            enumerator1.MoveNext();
            enumerator1.MoveNext();
            enumerator1.MoveNext();

            var enumerator2 = stream2.GetAsyncEnumerator();
            await enumerator2.MoveNextAsync();
            await enumerator2.MoveNextAsync();

            processed.Should().BeEquivalentTo(1);

            await enumerator2.MoveNextAsync();

            processed.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        public async Task PushAsync_WithNotMatchingStreams_ProcessedCallbackInvokedImmediately()
        {
            var processed = new List<object>();
            var provider = new MessageStreamProvider<object>(5)
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
            var provider = new MessageStreamProvider<int>(5)
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);
            await provider.CompleteAsync();

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
        public async Task CompleteAsync_PulledAllAndCompleted_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>(5)
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            completed.Should().BeFalse();

            using var enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.MoveNext();

            // The next MoveNext reaches the end of the enumerable
            // ReSharper disable once AccessToDisposedClosure
            Task.Run(() => enumerator.MoveNext()).RunWithoutBlocking();

            completed.Should().BeFalse();

            await provider.CompleteAsync();

            // Give the other thread a chance to exit the MoveNext and invoke the callback
            await Task.Delay(100);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_CompletedAndEnumerated_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>(5)
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);
            await provider.CompleteAsync();

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
        public async Task CompleteAsync_EnumeratedAndCompleted_EnumerationCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>(5)
            {
                EnumerationCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            completed.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNextAsync().RunWithoutBlocking();

            completed.Should().BeFalse();

            await provider.CompleteAsync();

            // Give the other thread a chance to exit the MoveNext and invoke the callback
            await Task.Delay(100);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_PushCompletedCallbackInvoked()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>(5)
            {
                PushCompletedCallback = () =>
                {
                    completed = true;
                    return Task.CompletedTask;
                }
            };

            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            completed.Should().BeFalse();

            var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();

            // The next MoveNext reaches the end of the enumerable
            enumerator.MoveNextAsync().RunWithoutBlocking();

            completed.Should().BeFalse();

            await provider.CompleteAsync();

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_WhileEnumeratingStreams_EnumerationCompleted()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>(5);
            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

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

            await provider.CompleteAsync();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => completed);

            completed.Should().BeTrue();
        }

        [Fact]
        public async Task CompleteAsync_WhileAsyncEnumeratingStreams_EnumerationCompleted()
        {
            var completed = false;
            var provider = new MessageStreamProvider<int>(5);
            var stream = provider.CreateStream<int>();

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            var enumerator = stream.GetAsyncEnumerator();
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

            await provider.CompleteAsync();

            // Give the other thread a chance to exit the MoveNext
            await AsyncTestingUtil.WaitAsync(() => completed);

            completed.Should().BeTrue();
        }

        [Fact]
        public void CreateStream_CalledTwiceForSameType_NewInstanceReturned()
        {
            var provider = new MessageStreamProvider<IMessage>(5);
            var stream1 = provider.CreateStream<IEvent>();
            var stream2 = provider.CreateStream<IEvent>();

            stream2.Should().NotBeSameAs(stream1);
        }

        [Fact]
        public void CreateStream_GenericAndNonGenericVersions_EquivalentInstanceReturned()
        {
            var provider = new MessageStreamProvider<IMessage>(5);

            var stream1 = provider.CreateStream<IEvent>();
            var stream2 = provider.CreateStream(typeof(IEvent));

            stream2.Should().BeOfType(stream1.GetType());
        }
    }
}
