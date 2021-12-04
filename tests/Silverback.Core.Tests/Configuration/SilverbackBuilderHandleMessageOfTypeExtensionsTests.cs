// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

public class SilverbackBuilderHandleMessageOfTypeExtensionsTests
{
    [Fact]
    public void HandleMessageOfType_Type_MessagesRepublished()
    {
        int received = 0;

        static UnhandledMessage Republish(TestEventOne message) => new();
        void Receive(UnhandledMessage message) => received++;

        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber((Func<TestEventOne, UnhandledMessage>)Republish)
                .AddDelegateSubscriber((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType(typeof(UnhandledMessage)));

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        received.Should().Be(2);
    }

    [Fact]
    public void HandleMessageOfType_Type_MessagesEnumerableRepublished()
    {
        int received = 0;

        static IEnumerable<UnhandledMessage> Republish(TestEventOne message) =>
            new[] { new UnhandledMessage(), new UnhandledMessage() };

        void Receive(UnhandledMessage message) => received++;

        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber((Func<TestEventOne, IEnumerable<UnhandledMessage>>)Republish)
                .AddDelegateSubscriber((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType(typeof(UnhandledMessage)));

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        received.Should().Be(4);
    }

    [Fact]
    public void HandleMessageOfType_TypeGenericParameter_MessagesRepublished()
    {
        int received = 0;

        static UnhandledMessage Republish(TestEventOne message) => new();
        void Receive(UnhandledMessage message) => received++;

        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber((Func<TestEventOne, UnhandledMessage>)Republish)
                .AddDelegateSubscriber((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType<UnhandledMessage>());

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        received.Should().Be(2);
    }

    [Fact]
    public void HandleMessageOfType_BaseType_MessagesRepublished()
    {
        int received = 0;

        static UnhandledMessage Republish(TestEventOne message) => new();
        void Receive(UnhandledMessage message) => received++;

        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber((Func<TestEventOne, UnhandledMessage>)Republish)
                .AddDelegateSubscriber((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType(typeof(BaseUnhandledMessage)));

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        received.Should().Be(2);
    }

    [Fact]
    public void HandleMessageOfType_BaseTypeGenericParameter_MessagesRepublished()
    {
        int received = 0;

        static UnhandledMessage Republish(TestEventOne message) => new();
        void Receive(UnhandledMessage message) => received++;

        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber((Func<TestEventOne, UnhandledMessage>)Republish)
                .AddDelegateSubscriber((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType<BaseUnhandledMessage>());

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        received.Should().Be(2);
    }

    [Fact]
    public void HandleMessageOfType_Interface_MessagesRepublished()
    {
        int received = 0;

        static UnhandledMessage Republish(TestEventOne message) => new();
        void Receive(UnhandledMessage message) => received++;

        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber((Func<TestEventOne, UnhandledMessage>)Republish)
                .AddDelegateSubscriber((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType(typeof(IUnhandledMessage)));

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        received.Should().Be(2);
    }

    [Fact]
    public void HandleMessageOfType_InterfaceGenericParameter_MessagesRepublished()
    {
        int received = 0;

        static UnhandledMessage Republish(TestEventOne message) => new();
        void Receive(UnhandledMessage message) => received++;

        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber((Func<TestEventOne, UnhandledMessage>)Republish)
                .AddDelegateSubscriber((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType<IUnhandledMessage>());

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        received.Should().Be(2);
    }

    private static IPublisher GetPublisher(Action<SilverbackBuilder> buildAction)
    {
        ServiceCollection services = new();
        SilverbackBuilder builder = services.AddSilverback();

        services.AddLoggerSubstitute();

        buildAction(builder);

        return services.BuildServiceProvider().GetRequiredService<IPublisher>();
    }
}
