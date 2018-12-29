// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Broker
{
    [TestFixture]
    public class BrokerTest
    {
        private TestBroker _broker;

        [SetUp]
        public void Setup()
        {
            _broker = new TestBroker();
        }

        [Test]
        public void GetProducerTest()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);

            Assert.That(producer, Is.Not.Null);
        }

        [Test]
        public void GetCachedProducerTest()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);
            var producer2 = _broker.GetProducer(TestEndpoint.Default);

            Assert.That(producer2, Is.SameAs(producer));
        }

        [Test]
        public void GetProducerForDifferentEndpointTest()
        {
            var producer = _broker.GetConsumer(TestEndpoint.Default);
            var producer2 = _broker.GetConsumer(new TestEndpoint("test2"));

            Assert.That(producer2, Is.Not.SameAs(producer));
        }

        [Test]
        public void GetConsumerTest()
        {
            var consumer = _broker.GetConsumer(TestEndpoint.Default);

            Assert.That(consumer, Is.Not.Null);
        }

        [Test]
        public void GetCachedConsumerTest()
        {
            var consumer = _broker.GetConsumer(TestEndpoint.Default);
            var consumer2 = _broker.GetConsumer(TestEndpoint.Default);

            Assert.That(consumer2, Is.SameAs(consumer));
        }

        [Test]
        public void GetConsumerForDifferentEndpointTest()
        {
            var consumer = _broker.GetConsumer(TestEndpoint.Default);
            var consumer2 = _broker.GetConsumer(new TestEndpoint("test2"));

            Assert.That(consumer2, Is.Not.SameAs(consumer));
        }

        [Test]
        public void Produce_IntegrationMessage_IdIsSet()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);

            var message = new TestEventOne();

            producer.Produce(message);

            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public async Task ProduceAsync_IntegrationMessage_IdIsSet()
        {
            var producer = _broker.GetProducer(TestEndpoint.Default);

            var message = new TestEventOne();

            await producer.ProduceAsync(message);

            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
        }

    }
}
