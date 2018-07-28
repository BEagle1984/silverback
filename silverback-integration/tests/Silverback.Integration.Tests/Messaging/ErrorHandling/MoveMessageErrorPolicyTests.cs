using System;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class MoveMessageErrorPolicyTests
    {
        private IBus _bus;
        private readonly IEndpoint _endpoint = BasicEndpoint.Create("test_target");

        [SetUp]
        public void Setup()
        {
            _bus = new BusBuilder().Build()
                .ConfigureBroker<TestBroker>(x => x
                    .UseServer("server")
                );
        }

        [Test]
        public void SuccessTest()
        {
            var executed = false;
            var policy = ErrorPolicy.Move(_endpoint);

            policy.Init(_bus);
            _bus.ConnectBrokers();

            policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => executed = true);

            var producer = (TestProducer)_bus.GetBroker().GetProducer(BasicEndpoint.Create("test"));

            Assert.That(executed, Is.True);
            Assert.That(producer.SentMessages.Count, Is.EqualTo(0));
        }

        [Test]
        public void ErrorTest()
        {
            var policy = ErrorPolicy.Move(_endpoint);

            policy.Init(_bus);
            _bus.ConnectBrokers();

            policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => throw new Exception("test"));

            var producer = (TestProducer)_bus.GetBroker().GetProducer(_endpoint);

            Assert.That(producer.SentMessages.Count, Is.EqualTo(1));
        }
    }
}