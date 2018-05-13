//using System;
//using System.Collections.Generic;
//using System.Text;
//using NUnit.Framework;
//using Silverback.Tests.TestTypes.Domain;
//using Silverback.Tests.TestTypes.Subscribers;

//namespace Silverback.Tests.Messaging
//{
//    [TestFixture]
//    public class MultiMessageHandlerTest
//    {
//        [SetUp]
//        public void Setup()
//        {
//            TestAllEventsHandler.CounterCommandOne = 0;
//            TestAllEventsHandler.CounterCommandTwo = 0;
//            TestAllEventsHandler.CounterFiltered = 0;
//        }

//        [Test]
//        public void RoutingTest()
//        {
//            var handler = new TestAllEventsHandler();

//            handler.Handle(new TestEventOne());
//            handler.Handle(new TestEventTwo());
//            handler.Handle(new TestEventOne());
//            handler.Handle(new TestEventOne());
//            handler.Handle(new TestEventTwo());

//            Assert.That(TestAllEventsHandler.CounterCommandOne, Is.EqualTo(3));
//            Assert.That(TestAllEventsHandler.CounterCommandTwo, Is.EqualTo(2));
//        }

//        [Test]
//        public void FilteringTest()
//        {
//            var handler = new TestAllEventsHandler();

//            handler.Handle(new TestEventOne());
//            handler.Handle(new TestEventOne { Message = "skip" });
//            handler.Handle(new TestEventOne());

//            Assert.That(TestAllEventsHandler.CounterFiltered, Is.EqualTo(2));
//        }
//    }
//}
