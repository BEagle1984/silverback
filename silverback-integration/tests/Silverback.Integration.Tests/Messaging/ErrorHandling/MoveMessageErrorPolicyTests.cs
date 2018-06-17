using System;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class MoveMessageErrorPolicyTests
    {
        private IBroker _broker;
        private IEndpoint _endpoint = BasicEndpoint.Create("test_target");

        [SetUp]
        public void Setup()
        {
            _broker = new TestBroker();
        }

        [Test]
        public void SuccessTest()
        {
            var executed = false;
            var policy = new MoveMessageErrorPolicy(_broker, _endpoint);

            _broker.Connect();

            policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => executed = true);

            var producer = (TestProducer)_broker.GetProducer(BasicEndpoint.Create("test"));

            Assert.That(executed, Is.True);
            Assert.That(producer.SentMessages.Count, Is.EqualTo(0));
        }

        [Test]
        public void ErrorTest()
        {
            var policy = new MoveMessageErrorPolicy(_broker, _endpoint);
            
            _broker.Connect();

            policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => throw new Exception("test"));

            var producer = (TestProducer)_broker.GetProducer(_endpoint);

            Assert.That(producer.SentMessages.Count, Is.EqualTo(1));
        }
    }
}