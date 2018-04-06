using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class CommandPublisherTests
    {
        [Test]
        public void SendCommandTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                var publisher = bus.GetCommandPublisher<ICommand>();

                publisher.Send(new TestCommandOne());
                publisher.Send(new TestCommandTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public void SendSpecificCommandTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                var publisher = bus.GetCommandPublisher<TestCommandOne>();

                publisher.Send(new TestCommandOne());

                Assert.That(counter, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task SendCommandAsyncTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                var publisher = bus.GetCommandPublisher<ICommand>();

                await publisher.SendAsync(new TestCommandOne());
                await publisher.SendAsync(new TestCommandTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task SendSpecificCommandAsyncTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                var publisher = bus.GetCommandPublisher<TestCommandOne>();

                await publisher.SendAsync(new TestCommandOne());

                Assert.That(counter, Is.EqualTo(1));
            }
        }
    }
}