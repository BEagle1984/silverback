// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
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

            await provider.PushAsync(new TestEventOne(), false);
            await provider.PushAsync(new TestEventTwo(), false);
            await provider.PushAsync(new TestCommandOne(), false);

            await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

            await Task.WhenAll(task1, task2);

            eventsList.Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesList.Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public async Task PushAsync_MessageWithoutMatchingStream_ExceptionThrown()
        {
            var provider = new MessageStreamProvider<IMessage>();
            var stream = provider.CreateStream<TestEventOne>();

            Task.Run(() => stream.ToList()).RunWithoutBlocking();

            await provider.PushAsync(new TestEventOne());
            Func<Task> act = () => provider.PushAsync(new TestEventTwo());

            act.Should().Throw<UnhandledMessageException>();
        }

        [Fact]
        public async Task PushAsync_MessageWithoutMatchingStreamDisablingException_NoExceptionThrown()
        {
            var provider = new MessageStreamProvider<IMessage>();
            var stream = provider.CreateStream<TestEventOne>();

            Task.Run(() => stream.ToList()).RunWithoutBlocking();

            await provider.PushAsync(new TestEventOne());
            Func<Task> act = () => provider.PushAsync(new TestEventTwo(), false);

            act.Should().NotThrow();
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

            await provider.PushAsync(new TestEnvelope(new TestEventOne()), false);
            await provider.PushAsync(new TestEnvelope(new TestEventTwo()), false);
            await provider.PushAsync(new TestEnvelope(new TestCommandOne()), false);

            await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

            await Task.WhenAll(task1, task2);

            eventsList.Should().BeEquivalentTo(new TestEventOne(), new TestEventTwo());
            testEventOnesList.Should().BeEquivalentTo(new TestEventOne());
        }

        [Fact]
        public async Task PushAsync_EnvelopeWithoutMatchingStream_ExceptionThrown()
        {
            var provider = new MessageStreamProvider<IEnvelope>();
            var stream = provider.CreateStream<TestEventOne>();

            Task.Run(() => stream.ToList()).RunWithoutBlocking();

            await provider.PushAsync(new TestEnvelope(new TestEventOne()));
            Func<Task> act = () => provider.PushAsync(new TestEnvelope(new TestEventTwo()));

            act.Should().Throw<UnhandledMessageException>();
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
            provider.PushAsync(3).RunWithoutBlocking();

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

            await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
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
            provider.PushAsync(3).RunWithoutBlocking();

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

            await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
            pushTask2.IsCompleted.Should().BeTrue();
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
        public async Task Abort_WhileEnumeratingStream_EnumerationAborted()
        {
            int? count = null;
            var provider = new MessageStreamProvider<int>();
            var stream = provider.CreateStream<int>();

            var enumerationTask = Task.Run(() => { count = stream.Count(); });

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            count.Should().BeNull();

            provider.Abort();

            await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

            count.Should().BeNull();
            enumerationTask.Status.Should().Be(TaskStatus.Faulted);
        }

        [Fact]
        public async Task Abort_WhileAsyncEnumeratingStreams_EnumerationAborted()
        {
            int? count = null;
            var provider = new MessageStreamProvider<int>();
            var stream = provider.CreateStream<int>();

            var enumerationTask = Task.Run(async () => { count = await stream.CountAsync(); });

            await provider.PushAsync(1);
            await provider.PushAsync(2);
            await provider.PushAsync(3);

            count.Should().BeNull();

            provider.Abort();

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
