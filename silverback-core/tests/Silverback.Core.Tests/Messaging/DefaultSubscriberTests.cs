using System;
using NSubstitute;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class DefaultSubscriberTests
    {
        private IMessageHandler _mockHandler;
        private ITypeFactory _typeFactory;

        [SetUp]
        public void Setup()
        {
            _mockHandler = Substitute.For<IMessageHandler>();
            _typeFactory = Substitute.For<ITypeFactory>();
            _typeFactory.GetInstance(Arg.Any<Type>()).Returns(_mockHandler);
        }

        [Test]
        public void BasicTest()
        {
            using (var bus = new Bus())
            {
                bus.Subscribe(o => new DefaultSubscriber<IMessage>(o, _typeFactory, typeof(IMessageHandler<IMessage>)));

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());

                _mockHandler.Received(3).Handle(Arg.Any<ICommand>());
                _mockHandler.Received(2).Handle(Arg.Any<TestCommandOne>());
                _mockHandler.Received(1).Handle(Arg.Any<TestCommandTwo>());
            }
        }

        [Test]
        public void FilteringTest()
        {
            using (var bus = new Bus())
            {
                bus.Subscribe(o => new DefaultSubscriber<IMessage>(o, _typeFactory, typeof(IMessageHandler<IMessage>),
                    m => m is TestCommandTwo && ((TestCommandTwo)m).Message == "A"));

                bus.Publish(new TestCommandTwo { Message = "B" });
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo { Message = "A" });
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo { Message = "B" });
                bus.Publish(new TestCommandTwo { Message = "A" });
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandOne());

                _mockHandler.DidNotReceive().Handle(Arg.Any<TestCommandOne>());
                _mockHandler.Received(2).Handle(Arg.Any<TestCommandTwo>());
            }
        }
    }
}
