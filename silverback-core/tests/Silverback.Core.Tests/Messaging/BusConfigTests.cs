using System;
using NSubstitute;
using NUnit.Framework;
using Silverback.Core.Tests.TestTypes.Handlers;
using Silverback.Core.Tests.TestTypes.Subscribers;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class BusConfigTests
    {
        [Test]
        public void SubscribeHandlerMethodTest()
        {
            using (var bus = new Bus())
            {
                int counterOne = 0;
                int counterTwo = 0;

                bus.Config()
                    .Subscribe<TestCommandOne>(m => counterOne++)
                    .Subscribe<TestCommandTwo>(m => counterTwo++);

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(counterOne, Is.EqualTo(2));
                Assert.That(counterTwo, Is.EqualTo(3));
            }
        }

        [Test]
        public void SubscribeHandlerTest()
        {
            using (var bus = new Bus())
            {
                TestCommandOneHandler.Counter = 0;
                TestCommandTwoHandler.Counter = 0;

                bus.Config()
                    .WithFactory(t => (IMessageHandler)Activator.CreateInstance(t))
                    .Subscribe<TestCommandOne, TestCommandOneHandler>()
                    .Subscribe<TestCommandTwo, TestCommandTwoHandler>();

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(TestCommandOneHandler.Counter, Is.EqualTo(2));
                Assert.That(TestCommandTwoHandler.Counter, Is.EqualTo(3));
            }
        }

        [Test]
        public void SubscribeCustomSubscriberTest()
        {
            using (var bus = new Bus())
            {
                TestCommandOneSubscriber.Counter = 0;
                TestCommandTwoSubscriber.Counter = 0;

                bus.Config()
                    .Subscribe<TestCommandOne>(o => new TestCommandOneSubscriber(o))
                    .Subscribe<TestCommandTwo>(o => new TestCommandTwoSubscriber(o));

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(TestCommandOneSubscriber.Counter, Is.EqualTo(2));
                Assert.That(TestCommandTwoSubscriber.Counter, Is.EqualTo(3));
            }
        }
    }
}
