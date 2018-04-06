using NSubstitute;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Adapters
{
    [TestFixture]
    public class GenericMessageTranslatorTests
    {
        private IBus _mockBus;

        [SetUp]
        public void Setup()
        {
            _mockBus = Substitute.For<IBus>();
        }

        [Test]
        public void HandleTest()
        {
            var translator =
                new GenericMessageTranslator<TestInternalEventOne, TestEventOne>(m =>
                    new TestEventOne { Content = m.InternalMessage }, _mockBus);

            var input = new TestInternalEventOne { InternalMessage = "abcd" };
            translator.Handle(input);

            _mockBus.Received(1).Publish(Arg.Any<IMessage>());
            _mockBus.Received(1).Publish(Arg.Any<TestEventOne>());
            _mockBus.DidNotReceive().Publish(Arg.Any<TestInternalEventOne>());
        }

        [Test]
        public void MapTest()
        {
            var translator =
                new GenericMessageTranslator<TestInternalEventOne, TestEventOne>(m =>
                    new TestEventOne { Content = m.InternalMessage }, _mockBus);

            TestEventOne output = null;
            _mockBus.When(b => b.Publish(Arg.Any<TestEventOne>())).Do(i => output = i.Arg<TestEventOne>());

            var input = new TestInternalEventOne { InternalMessage = "abcd" };
            translator.Handle(input);

            Assert.That(output, Is.Not.Null);
            Assert.That(output.Content, Is.EqualTo(input.InternalMessage));
        }
    }
}
