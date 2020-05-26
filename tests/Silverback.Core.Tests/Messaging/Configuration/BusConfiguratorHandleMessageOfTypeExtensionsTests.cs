// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class BusConfiguratorHandleMessageOfTypeExtensionsTests
    {
        private readonly IBusConfigurator _busConfigurator;

        private readonly IPublisher _publisher;

        public BusConfiguratorHandleMessageOfTypeExtensionsTests()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .Services.BuildServiceProvider();

            _busConfigurator = serviceProvider.GetRequiredService<IBusConfigurator>();

            var scopedServiceProvider = serviceProvider.CreateScope().ServiceProvider;
            _publisher = scopedServiceProvider.GetRequiredService<IPublisher>();
        }

        [Fact]
        public void HandleMessageOfType_Type_MessagesRepublished()
        {
            int received = 0;

            static UnhandledMessage Republish(TestEventOne message) => new UnhandledMessage();
            void Receive(UnhandledMessage message) => received++;

            _busConfigurator
                .Subscribe((Func<TestEventOne, UnhandledMessage>)Republish)
                .Subscribe((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType(typeof(UnhandledMessage));

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void HandleMessageOfType_Type_MessagesEnumerableRepublished()
        {
            int received = 0;

            static IEnumerable<UnhandledMessage> Republish(TestEventOne message) =>
                new[] { new UnhandledMessage(), new UnhandledMessage() };

            void Receive(UnhandledMessage message) => received++;

            _busConfigurator
                .Subscribe((Func<TestEventOne, IEnumerable<UnhandledMessage>>)Republish)
                .Subscribe((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType(typeof(UnhandledMessage));

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(4);
        }

        [Fact]
        public void HandleMessageOfType_TypeGenericParameter_MessagesRepublished()
        {
            int received = 0;

            static UnhandledMessage Republish(TestEventOne message) => new UnhandledMessage();
            void Receive(UnhandledMessage message) => received++;

            _busConfigurator
                .Subscribe((Func<TestEventOne, UnhandledMessage>)Republish)
                .Subscribe((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType<UnhandledMessage>();

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void HandleMessageOfType_BaseType_MessagesRepublished()
        {
            int received = 0;

            static UnhandledMessage Republish(TestEventOne message) => new UnhandledMessage();
            void Receive(UnhandledMessage message) => received++;

            _busConfigurator
                .Subscribe((Func<TestEventOne, UnhandledMessage>)Republish)
                .Subscribe((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType(typeof(BaseUnhandledMessage));

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void HandleMessageOfType_BaseTypeGenericParameter_MessagesRepublished()
        {
            int received = 0;

            static UnhandledMessage Republish(TestEventOne message) => new UnhandledMessage();
            void Receive(UnhandledMessage message) => received++;

            _busConfigurator
                .Subscribe((Func<TestEventOne, UnhandledMessage>)Republish)
                .Subscribe((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType<BaseUnhandledMessage>();

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void HandleMessageOfType_Interface_MessagesRepublished()
        {
            int received = 0;

            static UnhandledMessage Republish(TestEventOne message) => new UnhandledMessage();
            void Receive(UnhandledMessage message) => received++;

            _busConfigurator
                .Subscribe((Func<TestEventOne, UnhandledMessage>)Republish)
                .Subscribe((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType(typeof(IUnhandledMessage));

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void HandleMessageOfType_InterfaceGenericParameter_MessagesRepublished()
        {
            int received = 0;

            static UnhandledMessage Republish(TestEventOne message) => new UnhandledMessage();
            void Receive(UnhandledMessage message) => received++;

            _busConfigurator
                .Subscribe((Func<TestEventOne, UnhandledMessage>)Republish)
                .Subscribe((Action<UnhandledMessage>)Receive)
                .HandleMessagesOfType<IUnhandledMessage>();

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }
    }
}
