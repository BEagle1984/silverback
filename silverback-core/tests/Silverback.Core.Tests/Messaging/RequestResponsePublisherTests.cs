using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class RequestResponsePublisherTests
    {
        [Test]
        public void GetResponseTest()
        {
            using (var bus = new Bus())
            {
                var requestPublisher = bus.GetRequestPublisher<IRequest, IResponse>();
                var responsePublisher = bus.GetResponsePublisher<IResponse>();

                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m =>
                {
                    switch (m)
                    {
                        case TestRequestOne req:
                            counter++;
                            responsePublisher.Reply(new TestResponseOne { Message = "one", RequestId = req.RequestId });
                            break;
                        case TestRequestTwo req:
                            counter++;
                            responsePublisher.Reply(new TestResponseTwo { Message = "two", RequestId = req.RequestId });
                            break;
                    }
                }));

                var responseOne = requestPublisher.GetResponse(new TestRequestOne()) as TestResponseOne;
                var responseTwo = requestPublisher.GetResponse(new TestRequestTwo()) as TestResponseTwo;

                Assert.That(counter, Is.EqualTo(2));
                Assert.That(responseOne, Is.Not.Null);
                Assert.That(responseOne.Message, Is.EqualTo("one"));
                Assert.That(responseTwo, Is.Not.Null);
                Assert.That(responseTwo.Message, Is.EqualTo("two"));
            }
        }

        [Test]
        public void GetSpecificResponseTest()
        {
            using (var bus = new Bus())
            {
                var requestPublisher = bus.GetRequestPublisher<TestRequestOne, TestResponseOne>();
                var responsePublisher = bus.GetResponsePublisher<TestResponseOne>();

                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m =>
                {
                    switch (m)
                    {
                        case TestRequestOne req:
                            counter++;
                            responsePublisher.Reply(new TestResponseOne { Message = "one", RequestId = req.RequestId });
                            break;
                    }
                }));

                var responseOne = requestPublisher.GetResponse(new TestRequestOne());

                Assert.That(counter, Is.EqualTo(1));
                Assert.That(responseOne, Is.Not.Null);
                Assert.That(responseOne.Message, Is.EqualTo("one"));
            }
        }

        [Test]
        public async Task GetResponseAsyncTest()
        {
            using (var bus = new Bus())
            {
                var requestPublisher = bus.GetRequestPublisher<IRequest, IResponse>();
                var responsePublisher = bus.GetResponsePublisher<IResponse>();

                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m =>
                {
                    switch (m)
                    {
                        case TestRequestOne req:
                            counter++;
                            responsePublisher.ReplyAsync(new TestResponseOne { Message = "one", RequestId = req.RequestId });
                            break;
                        case TestRequestTwo req:
                            counter++;
                            responsePublisher.ReplyAsync(new TestResponseTwo { Message = "two", RequestId = req.RequestId });
                            break;
                    }
                }));

                var responseOne = await requestPublisher.GetResponseAsync(new TestRequestOne()) as TestResponseOne;
                var responseTwo = await requestPublisher.GetResponseAsync(new TestRequestTwo()) as TestResponseTwo;

                Assert.That(counter, Is.EqualTo(2));
                Assert.That(responseOne, Is.Not.Null);
                Assert.That(responseOne.Message, Is.EqualTo("one"));
                Assert.That(responseTwo, Is.Not.Null);
                Assert.That(responseTwo.Message, Is.EqualTo("two"));
            }
        }

        [Test]
        public async Task GetSpecificResponseAsyncTest()
        {
            using (var bus = new Bus())
            {
                var requestPublisher = bus.GetRequestPublisher<TestRequestOne, TestResponseOne>();
                var responsePublisher = bus.GetResponsePublisher<TestResponseOne>();

                var counter = 0;
                bus.Subscribe(o => o.Subscribe(m =>
                {
                    switch (m)
                    {
                        case TestRequestOne req:
                            counter++;
                            responsePublisher.ReplyAsync(new TestResponseOne { Message = "one", RequestId = req.RequestId });
                            break;
                    }
                }));

                var responseOne = await requestPublisher.GetResponseAsync(new TestRequestOne());

                Assert.That(counter, Is.EqualTo(1));
                Assert.That(responseOne, Is.Not.Null);
                Assert.That(responseOne.Message, Is.EqualTo("one"));
            }
        }
    }
}