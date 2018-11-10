using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Integration;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Integration
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
            var translator =
                new GenericMessageTranslator<TestInternalEventOne, TestEventOne>(m =>
                    new TestEventOne { Content = m.InternalMessage });

            var input = new TestInternalEventOne { InternalMessage = "abcd" };
            translator.OnNext(input, _mockBus);

            _mockBus.Received(1).Publish(Arg.Any<IMessage>());
            _mockBus.Received(1).Publish(Arg.Any<TestEventOne>());
            _mockBus.DidNotReceive().Publish(Arg.Any<TestInternalEventOne>());
        }

        [Test]
        public void MapTest()
        {
            var translator =
                new GenericMessageTranslator<TestInternalEventOne, TestEventOne>(m =>
                    new TestEventOne { Content = m.InternalMessage });

            TestEventOne output = null;
            _mockBus.When(b => b.Publish(Arg.Any<TestEventOne>())).Do(i => output = i.Arg<TestEventOne>());

            var input = new TestInternalEventOne { InternalMessage = "abcd" };
            translator.OnNext(input, _mockBus);

            Assert.That(output, Is.Not.Null);
            Assert.That(output.Content, Is.EqualTo(input.InternalMessage));
        }
    }
}
