// Copyright (c) 2018 Laurent Bovet
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Core.Messaging;
using Silverback.Core.Rx.Tests.TestTypes;
using Silverback.Core.Rx.Tests.TestTypes.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Core.Rx.Tests.Messaging
{
    public class TestSubscriber : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        public void OnTestMessageReceived(TestEventTwo message)
        {
            ReceivedMessagesCount++;
        }
    }

    public abstract class RxAdapter<InMessage, OutMessage> : ISubscriber where InMessage : IMessage where OutMessage : IMessage {       

        public IPublisher Publisher;

        [Subscribe]
        public async Task OnMessageReceived(InMessage message)
        {
            await Observable
                .Return(message)
                .SelectMany(this.process)
                .SelectMany(async x => {
                    await Publisher.PublishAsync(x); return true;
                });        
        }

        public abstract IObservable<OutMessage> process(InMessage message); 
    }

    public class TestProcessor : RxAdapter<TestEventOne, TestEventTwo> 
    {
        public override IObservable<TestEventTwo> process(TestEventOne message) 
        {
            return Observable
                .Range(1,2)
                .Delay(TimeSpan.FromMilliseconds(500))
                .Select(x => new TestEventTwo());                
        }
    }

    [Collection("Rx")]
    public class RxAdapterTests
    {
        private TestSubscriber _testSubscriber = new TestSubscriber();
        private TestProcessor _testProcessor = new TestProcessor();

        [Fact]
        public async Task RxAdapter()
        {
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(_testSubscriber, _testProcessor), new NullLogger<Publisher>());
            _testProcessor.Publisher = publisher;

            await publisher.PublishAsync(new TestEventOne());            
            Assert.Equal(2, _testSubscriber.ReceivedMessagesCount);

            await publisher.PublishAsync(new TestEventOne());
            Assert.Equal(4, _testSubscriber.ReceivedMessagesCount);
        }
    }
}
