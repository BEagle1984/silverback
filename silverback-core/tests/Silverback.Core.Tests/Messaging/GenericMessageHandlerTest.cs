using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Tests.TestTypes.Handlers;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class GenericMessageHandlerTest
    {
        [Test]
        public void HandleTest()
        {
            int counter = 0;
            IMessageHandler handler = new GenericMessageHandler<TestCommandOne>(m => counter++);

            handler.Handle(new TestCommandOne());

            Assert.That(counter, Is.EqualTo(1));
        }

        [Test]
        public void HandleByTypeTest()
        {
            int counter = 0;
            IMessageHandler handler = new GenericMessageHandler<TestCommandOne>(m => counter++);

            handler.Handle(new TestCommandOne());
            handler.Handle(new TestCommandTwo());
            handler.Handle(new TestCommandOne());

            Assert.That(counter, Is.EqualTo(2));
        }

        [Test]
        public void HandleFilterTest()
        {
            int counter = 0;
            IMessageHandler handler = new GenericMessageHandler<TestCommandOne>(m => counter++, m => m.Message != "skip");

            handler.Handle(new TestCommandOne());
            handler.Handle(new TestCommandOne { Message = "skip" });
            handler.Handle(new TestCommandOne { Message = "abc" });

            Assert.That(counter, Is.EqualTo(2));
        }
    }
}
