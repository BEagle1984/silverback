using System;
using System.Threading.Tasks;
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
            using (var bus = new Bus())
            {
                var subscriber = new TestCommandOneSubscriber();
                bus.Config()
                    .Subscribe(subscriber);

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

            using (var bus = new Bus())
            {
                bus.Config()
                    .WithFactory(t =>
                    {
                        if (t == typeof(TestCommandOneSubscriber))
                            return subscriberOne;
                        if (t == typeof(TestCommandTwoAsyncSubscriber))
                            return subscriberTwo;

                        throw new ArgumentOutOfRangeException();
                    })
                    .Subscribe<TestCommandOneSubscriber>()
                    .Subscribe<TestCommandTwoAsyncSubscriber>();

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(subscriberOne.Handled, Is.EqualTo(2));
                Assert.That(subscriberTwo.Handled, Is.EqualTo(3));
            }
        }

        [Test]
        public void SubscribeWithDefaultFactoryTest()
        {
            using (var bus = new Bus())
            {
                bus.Config()
                    .WithDefaultFactory()
                    .Subscribe<TestCommandOneSubscriber>();

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandOne());

                // Cannot really assert much, but no exception is good already.
            }
        }

        [Test]
        public void SubscribeHandlerMethodTest()
        {
            using (var bus = new Bus())
            {
                int counterOne = 0;
                int counterTwo = 0;

                bus.Config()
                    .Subscribe<TestCommandOne>(m => counterOne++)
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
            using (var bus = new Bus())
            {
                int counter = 0;

                bus.Config().Subscribe(m => counter++);

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
            using (var bus = new Bus())
            {
                bus.Config()
                    .ConfigureUsing<FakeConfigurator>();

                Assert.That(FakeConfigurator.Executed, Is.True);
            }
        }

        [Test]
        public void MessageHierarchyTest()
        {
            var counterBase = 0;
            var counterA = 0;
            var counterB = 0;
            using (var bus = new Bus())
            {
                bus.Config().Subscribe<TestEventHierarchyBase>(m => counterBase++);
                bus.Config().Subscribe<TestEventHierarchyChildA>(m => counterA++);
                bus.Config().Subscribe<TestEventHierarchyChildB>(m => counterB++);

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
