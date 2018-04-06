using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class PublisherTests
    {
        [Test]
        public void PublishEventTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                var publisher = bus.GetPublisher();

                publisher.Publish(new TestEventOne());
                publisher.Publish(new TestEventTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task PublishEventAsyncTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                var publisher = bus.GetPublisher();

                await publisher.PublishAsync(new TestEventOne());
                await publisher.PublishAsync(new TestEventTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public void SendCommandTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                var publisher = bus.GetPublisher();

                publisher.Send(new TestCommandOne());
                publisher.Send(new TestCommandTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task SendCommandAsyncTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m => counter++));

                var publisher = bus.GetPublisher();

                await publisher.SendAsync(new TestCommandOne());
                await publisher.SendAsync(new TestCommandTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public void GetResponseTest()
        {
            using (var bus = new Bus())
            {
                var publisher = bus.GetPublisher();

                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m =>
                {
                    switch (m)
                    {
                        case TestRequestOne req:
                            counter++;
                            publisher.Reply(new TestResponseOne { Message = "one", RequestId = req.RequestId });
                            break;
                        case TestRequestTwo req:
                            counter++;
                            publisher.Reply(new TestResponseTwo { Message = "two", RequestId = req.RequestId });
                            break;
                    }
                }));

                var responseOne = publisher.GetResponse<TestRequestOne, TestResponseOne>(new TestRequestOne());
                var responseTwo = publisher.GetResponse<TestRequestTwo, TestResponseTwo>(new TestRequestTwo());

                Assert.That(counter, Is.EqualTo(2));
                Assert.That(responseOne, Is.Not.Null);
                Assert.That(responseOne.Message, Is.EqualTo("one"));
                Assert.That(responseTwo, Is.Not.Null);
                Assert.That(responseTwo.Message, Is.EqualTo("two"));
            }
        }

        [Test]
        public async Task GetResponseAsyncTest()
        {
            using (var bus = new Bus())
            {
                var publisher = bus.GetPublisher();

                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m =>
                {
                    switch (m)
                    {
                        case TestRequestOne req:
                            counter++;
                            publisher.ReplyAsync(new TestResponseOne { Message = "one", RequestId = req.RequestId });
                            break;
                        case TestRequestTwo req:
                            counter++;
                            publisher.ReplyAsync(new TestResponseTwo { Message = "two", RequestId = req.RequestId });
                            break;
                    }
                }));

                var responseOne = await publisher.GetResponseAsync<TestRequestOne, TestResponseOne>(new TestRequestOne());
                var responseTwo = await publisher.GetResponseAsync<TestRequestTwo, TestResponseTwo>(new TestRequestTwo());

                Assert.That(counter, Is.EqualTo(2));
                Assert.That(responseOne, Is.Not.Null);
                Assert.That(responseOne.Message, Is.EqualTo("one"));
                Assert.That(responseTwo, Is.Not.Null);
                Assert.That(responseTwo.Message, Is.EqualTo("two"));
            }
        }
    }
}
