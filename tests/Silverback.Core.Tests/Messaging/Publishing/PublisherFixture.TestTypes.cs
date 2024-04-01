// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    private interface IEvent : IMessage;

    private interface ICommand : IMessage;

    private interface IQuery;

    private interface ITestRawEnvelope;

    private class TestEvent : IEvent;

    private class TestEventOne : TestEvent
    {
        public string? Message { get; init; }
    }

    private class TestEventTwo : TestEvent;

    private class TestCommandOne : ICommand;

    private class TestCommandTwo : ICommand;

    private class TestQueryOne : IQuery;

    private class TestQueryTwo : IQuery;

    private class TestQueryThree : IQuery;

    private class TestEnvelope : IEnvelope, ITestRawEnvelope
    {
        public TestEnvelope(object? message, bool autoUnwrap = true)
        {
            Message = message;
            AutoUnwrap = autoUnwrap;
        }

        public bool AutoUnwrap { get; }

        public object? Message { get; }
    }

    private class TestSubscriber<TMessage>
    {
        public TestingCollection<TMessage> ReceivedMessages { get; } = [];

        [Subscribe]
        [UsedImplicitly]
        public void Subscriber(TMessage message) => ReceivedMessages.Add(message);
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private class TestSubscriber : TestSubscriber<object>;
}
