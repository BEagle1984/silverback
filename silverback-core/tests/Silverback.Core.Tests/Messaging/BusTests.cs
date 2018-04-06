using System;
using System.Reactive.Linq;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class BusTests
    {
        [Test]
        public void PublishSubscribeBasicTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public void MultipleSubscribersTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                var counterOne = 0;
                var counterTwo = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));
                bus.Subscribe(o => o.OfType<TestCommandOne>().Subscribe(m => counterOne++));
                bus.Subscribe(o => o.OfType<TestCommandTwo>().Subscribe(m => counterTwo++));

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(counter, Is.EqualTo(5));
                Assert.That(counterOne, Is.EqualTo(2));
                Assert.That(counterTwo, Is.EqualTo(3));
            }
        }

        [Test]
        public void FilteringTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o
                    .Where(m => m is TestCommandTwo && ((TestCommandTwo) m).Message == "A")
                    .Subscribe(m => counter++));

                bus.Publish(new TestCommandTwo { Message = "B" });
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo { Message = "A" });
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo { Message = "B" });
                bus.Publish(new TestCommandTwo { Message = "A" });
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandOne());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        // TODO: Test lifecycle and unsubscribe
    }
}
