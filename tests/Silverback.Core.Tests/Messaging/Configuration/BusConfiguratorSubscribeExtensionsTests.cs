// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class BusConfiguratorSubscribeExtensionsTests
    {
        private readonly IBusConfigurator _busConfigurator;

        private readonly IPublisher _publisher;

        public BusConfiguratorSubscribeExtensionsTests()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .Services.BuildServiceProvider();

            _busConfigurator = serviceProvider.GetRequiredService<IBusConfigurator>();

            var scopedServiceProvider = serviceProvider.CreateScope().ServiceProvider;
            _publisher = scopedServiceProvider.GetRequiredService<IPublisher>();
        }

        private delegate void ReceiveTestEventOneDelegate(TestEventOne message);

        [Fact]
        public void Subscribe_Delegate_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(TestEventOne message) => received++;

            _busConfigurator.Subscribe((ReceiveTestEventOneDelegate)Receive);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_Action_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(TestEventOne message) => received++;

            _busConfigurator.Subscribe((Action<TestEventOne>)Receive);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_FuncReturningTask_MatchingMessagesReceived()
        {
            int received = 0;

            Task Receive(TestEventOne message)
            {
                received++;
                return Task.CompletedTask;
            }

            _busConfigurator.Subscribe((Func<TestEventOne, Task>)Receive);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_FuncReturningMessage_MessagesRepublished()
        {
            int received = 0;

            static TestEventTwo ReceiveOne(TestEventOne message) => new TestEventTwo();
            void ReceiveTwo(TestEventTwo message) => received++;

            _busConfigurator.Subscribe((Func<TestEventOne, object>)ReceiveOne);
            _busConfigurator.Subscribe((Action<TestEventTwo>)ReceiveTwo);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_FuncReturningTaskWithMessage_MessagesRepublished()
        {
            int received = 0;

            static Task<TestEventTwo> ReceiveOne(TestEventOne message) => Task.FromResult(new TestEventTwo());
            void ReceiveTwo(TestEventTwo message) => received++;

            _busConfigurator.Subscribe((Func<TestEventOne, object>)ReceiveOne);
            _busConfigurator.Subscribe((Action<TestEventTwo>)ReceiveTwo);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }
    }
}
