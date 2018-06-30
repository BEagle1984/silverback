using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes.Configuration;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class BusConfigTests
    {
        [Test]
        public void SubscribeTest()
        {
            using (var bus = new BusBuilder().Build())
            {
                var subscriber = new TestCommandOneSubscriber();
                bus.Subscribe(subscriber);

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandOne());

                Assert.That(subscriber.Handled, Is.EqualTo(2));
            }
        }

        [Test]
        public void SubscribeWithFactoryTest()
        {
            var subscriberOne = new TestCommandOneSubscriber();
            var subscriberTwo = new TestCommandTwoAsyncSubscriber();

            using (var bus = BuildBus())
            {
                bus.Subscribe<TestCommandOneSubscriber>()
                    .Subscribe<TestCommandTwoAsyncSubscriber>();

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(subscriberOne.Handled, Is.EqualTo(2));
                Assert.That(subscriberTwo.Handled, Is.EqualTo(3));
            }

            Bus BuildBus()
                => new BusBuilder()
                    .WithFactory(t =>
                    {
                        if (t == typeof(TestCommandOneSubscriber))
                            return subscriberOne;
                        if (t == typeof(TestCommandTwoAsyncSubscriber))
                            return subscriberTwo;

                        throw new ArgumentOutOfRangeException();
                    })
                    .Build();
        }

        [Test]
        public void SubscribeWithDefaultFactoryTest()
        {
            using (var bus = new BusBuilder().WithDefaultFactory().Build())
            {
                bus.Subscribe<TestCommandOneSubscriber>();

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandOne());

                // Cannot really assert much, but no exception is good already.
            }
        }

        [Test]
        public void SubscribeHandlerMethodTest()
        {
            using (var bus = new BusBuilder().Build())
            {
                int counterOne = 0;
                int counterTwo = 0;

                bus.Subscribe<TestCommandOne>(m => counterOne++)
                    .Subscribe<TestCommandTwo>(async m =>
                    {
                        await Task.Delay(1);
                        counterTwo++;
                    });

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
        public void SubscribeUntypedHandlerMethodTest()
        {
            using (var bus = new BusBuilder().Build())
            {
                int counter = 0;

                bus.Subscribe(m => counter++);

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(counter, Is.EqualTo(5));
            }
        }

        [Test]
        public void ConfigureUsingTest()
        {
            using (var bus = new BusBuilder().Build())
            {
                bus.ConfigureUsing<FakeConfigurator>();

                Assert.That(FakeConfigurator.Executed, Is.True);
            }
        }

        [Test]
        public void MessageHierarchyTest()
        {
            var counterBase = 0;
            var counterA = 0;
            var counterB = 0;
            using (var bus = new BusBuilder().Build())
            {
                bus.Subscribe<TestEventHierarchyBase>(m => counterBase++);
                bus.Subscribe<TestEventHierarchyChildA>(m => counterA++);
                bus.Subscribe<TestEventHierarchyChildB>(m => counterB++);

                bus.Publish(new TestEventHierarchyChildA());
                bus.Publish(new TestEventHierarchyChildB());
                bus.Publish(new TestEventHierarchyBase());
                bus.Publish(new TestEventHierarchyChildA());
                bus.Publish(new TestEventHierarchyChildA());

                Assert.That(counterBase, Is.EqualTo(5));
                Assert.That(counterA, Is.EqualTo(3));
                Assert.That(counterB, Is.EqualTo(1));
            }
        }
    }
}
