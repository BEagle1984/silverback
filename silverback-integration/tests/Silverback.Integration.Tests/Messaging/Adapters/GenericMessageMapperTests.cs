using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Adapters
{
    [TestFixture]
    public class GenericMessageMapperTests
    {
        private IBus _mockBus;

        [SetUp]
        public void Setup()
        {
            _mockBus = Substitute.For<IBus>();
            _mockBus.Items.Returns(new System.Collections.Concurrent.ConcurrentDictionary<string, object>(
                new Dictionary<string, object>
                {
                    {"Silverback.Messaging.Configuration.ILoggerFactory", NullLoggerFactory.Instance}
                }));
        }

        [Test]
        public void HandleTest()
        {
            var mapper =
                new GenericMessageMapper<TestInternalEventOne, TestEventOne>(m =>
                    new TestEventOne { Content = m.InternalMessage });
            mapper.Init(_mockBus);

            var input = new TestInternalEventOne { InternalMessage = "abcd" };
            mapper.Handle(input);

            _mockBus.Received(1).Publish(Arg.Any<IMessage>());
            _mockBus.Received(1).Publish(Arg.Any<TestEventOne>());
            _mockBus.DidNotReceive().Publish(Arg.Any<TestInternalEventOne>());
        }

        [Test]
        public void MapTest()
        {
            var mapper =
                new GenericMessageMapper<TestInternalEventOne, TestEventOne>(m =>
                    new TestEventOne { Content = m.InternalMessage });
            mapper.Init(_mockBus);

            TestEventOne output = null;
            _mockBus.When(b => b.Publish(Arg.Any<TestEventOne>())).Do(i => output = i.Arg<TestEventOne>());

            var input = new TestInternalEventOne { InternalMessage = "abcd" };
            mapper.Handle(input);

            Assert.That(output, Is.Not.Null);
            Assert.That(output.Content, Is.EqualTo(input.InternalMessage));
        }
    }
}
