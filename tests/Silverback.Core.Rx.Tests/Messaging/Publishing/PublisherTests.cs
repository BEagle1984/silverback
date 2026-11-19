// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.Rx.TestTypes.Messages;
using Silverback.Tests.Core.Rx.TestTypes.Messages.Base;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging.Publishing;

public class PublisherTests
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotReturnNullResults_WhenSubscriberReturnsNullObservable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AsObservable()
            .AddDelegateSubscriber<TestEventOne, IObservable<IMessage>?>(Handle1)
            .AddDelegateSubscriber<TestEventOne, IObservable<TestCommandOne>?>(Handle2)
            .AddDelegateSubscriber<TestEventOne, string>(Handle3));

        // With AsObservable, the returned IObservable<T> is republished: null means that there is nothing to republish
        static IObservable<IMessage>? Handle1(TestEventOne message) => null;
        static Task<IObservable<TestCommandOne>?> Handle2(TestEventOne message) => Task.FromResult<IObservable<TestCommandOne>?>(null);
        static string Handle3(TestEventOne message) => "result";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IObservable<IMessage>?> syncResults = publisher.Publish<IObservable<IMessage>?>(new TestEventOne());
        IReadOnlyCollection<IObservable<IMessage>?> asyncResults = await publisher.PublishAsync<IObservable<IMessage>?>(new TestEventOne());
        IReadOnlyCollection<object?> syncObjectResults = publisher.Publish<object?>(new TestEventOne());
        IReadOnlyCollection<object?> asyncObjectResults = await publisher.PublishAsync<object?>(new TestEventOne());

        syncResults.ShouldBeEmpty();
        asyncResults.ShouldBeEmpty();
        syncObjectResults.ShouldBe(["result"]);
        asyncObjectResults.ShouldBe(["result"]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublishEachMessage_WhenSubscriberReturnsObservable()
    {
        TestingCollection<object> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AsObservable()
            .AddDelegateSubscriber<TestEventOne, IObservable<ICommand>>(Route)
            .AddDelegateSubscriber<TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestCommandTwo>(Handle2));

        static IObservable<ICommand> Route(TestEventOne message) => new ICommand[] { new TestCommandOne(), new TestCommandTwo() }.ToObservable();
        void Handle1(TestCommandOne message) => republishedMessages.Add(message);
        void Handle2(TestCommandTwo message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Select(message => message.GetType()).ShouldBe(
            [typeof(TestCommandOne), typeof(TestCommandTwo), typeof(TestCommandOne), typeof(TestCommandTwo)]);
    }
}
