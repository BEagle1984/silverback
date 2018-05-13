//using System;
//using System.Collections.Generic;
//using System.Text;
//using NUnit.Framework;
//using Silverback.Messaging;
//using Silverback.Messaging.Messages;
//using Silverback.Tests.TestTypes.Domain;
//using Silverback.Tests.TestTypes.Subscribers;

//namespace Silverback.Tests.Messaging
//{
//    [TestFixture]
//    public class MessageHandlerTest
//    {
//        [SetUp]
//        public void Setup()
//        {
//            TestCommandOneHandler.Counter = 0;
//            TestCommandTwoHandler.Counter = 0;
//        }

//        [Test]
//        public void HandleTest()
//        {
//            IMessageHandler handler = new TestCommandOneHandler();

//            handler.Handle(new TestCommandOne());

//            Assert.That(TestCommandOneHandler.Counter, Is.EqualTo(1));
//        }

//        [Test]
//        public void HandleByTypeTest()
//        {
//            IMessageHandler handler = new TestCommandOneHandler();

//            handler.Handle(new TestCommandOne());
//            handler.Handle(new TestCommandTwo());
//            handler.Handle(new TestCommandOne());

//            Assert.That(TestCommandOneHandler.Counter, Is.EqualTo(2));
//        }

//        [Test]
//        public void HandleFilterTest()
//        {
//            IMessageHandler handler = new TestCommandOneHandler { Filter = m => m.Message != "skip" };

//            handler.Handle(new TestCommandOne());
//            handler.Handle(new TestCommandOne { Message = "skip" });
//            handler.Handle(new TestCommandOne { Message = "abc" });

//            Assert.That(TestCommandOneHandler.Counter, Is.EqualTo(2));
//        }
//    }
//}
