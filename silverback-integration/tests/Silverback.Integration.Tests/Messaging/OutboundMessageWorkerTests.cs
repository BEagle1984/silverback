using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class OutboundMessageWorkerTests
    {
        private OutboundMessagesRepository _repository;
        private TestBroker _broker;

        [SetUp]
        public void Setup()
        {
            _broker = new TestBroker().UseServer("server");
            _broker.Connect();

            _repository = new OutboundMessagesRepository();
        }

        // TODO: Test mismatching type versions

        [Test]
        public void SendPendingMessagesOnlyTest()
        {
            var endpoint = JsonConvert.SerializeObject(BasicEndpoint.Create("topic"));
            var endpoint2 = JsonConvert.SerializeObject(BasicEndpoint.Create("topic", "rat"));
            var message = JsonConvert.SerializeObject(new TestEventOne { Content = "Test" });
            var message2 = JsonConvert.SerializeObject(new TestEventOne { Content = "Test2" });
            var message3 = JsonConvert.SerializeObject(new TestEventOne { Content = "Test3" });
            _repository.Add(new OutboundMessageEntity
            {
                Endpoint = endpoint,
                EndpointType = typeof(BasicEndpoint).AssemblyQualifiedName,
                Message = message,
                MessageType = typeof(TestEventOne).AssemblyQualifiedName,
                Headers = null
            });
            _repository.Add(new OutboundMessageEntity
            {
                Endpoint = endpoint,
                EndpointType = typeof(BasicEndpoint).AssemblyQualifiedName,
                Message = message2,
                MessageType = typeof(TestEventOne).AssemblyQualifiedName,
                Headers = null,
                Sent = DateTime.UtcNow
            });
            _repository.Add(new OutboundMessageEntity
            {
                Endpoint = endpoint2,
                EndpointType = typeof(BasicEndpoint).AssemblyQualifiedName,
                Message = message3,
                MessageType = typeof(TestEventOne).AssemblyQualifiedName,
                Headers = null
            });

            var worker = new OutboundMessagesWorker<OutboundMessageEntity>(_repository, _broker);
            worker.SendPendingMessages();

            var sentMessages = _broker.SentMessages;

            Assert.That(sentMessages.Count, Is.EqualTo(2));

            var serializer = _broker.GetSerializer();
            var msg = serializer.Deserialize(sentMessages.First());
            var msg2 = serializer.Deserialize(sentMessages.Last());

            Assert.That(((TestEventOne)msg.Message).Content, Is.EqualTo("Test"));
            Assert.That(((TestEventOne)msg2.Message).Content, Is.EqualTo("Test3"));
        }

        [Test]
        public void DeserializePendingMessagesTest()
        {
            var endpoint = JsonConvert.SerializeObject(BasicEndpoint.Create("topic"));
            var message = JsonConvert.SerializeObject(new TestEventOne { Content = "Test" });
            var headers = JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            });
            _repository.Add(new OutboundMessageEntity
            {
                Endpoint = endpoint,
                EndpointType = typeof(BasicEndpoint).AssemblyQualifiedName,
                Message = message,
                MessageType = typeof(TestEventOne).AssemblyQualifiedName,
                Headers = headers
            });

            var worker = new OutboundMessagesWorker<OutboundMessageEntity>(_repository, _broker);
            worker.SendPendingMessages();

            var sentMessages =_broker.SentMessages;
            var serializer = _broker.GetSerializer();
            var msg = serializer.Deserialize(sentMessages.First());

            Assert.That(((TestEventOne)msg.Message).Content, Is.EqualTo("Test"));
            Assert.That(msg.Source, Is.EqualTo("testhost"));
        }

        [Test]
        public void SendMessagesOnceTest()
        {
            var endpoint = JsonConvert.SerializeObject(BasicEndpoint.Create("topic"));
            var message = JsonConvert.SerializeObject(new TestEventOne { Content = "Test" });
            var message2 = JsonConvert.SerializeObject(new TestEventOne { Content = "Test2" });
            var message3 = JsonConvert.SerializeObject(new TestEventOne { Content = "Test3" });
            _repository.Add(new OutboundMessageEntity
            {
                Endpoint = endpoint,
                EndpointType = typeof(BasicEndpoint).AssemblyQualifiedName,
                Message = message,
                MessageType = typeof(TestEventOne).AssemblyQualifiedName,
                Headers = null
            });
            _repository.Add(new OutboundMessageEntity
            {
                Endpoint = endpoint,
                EndpointType = typeof(BasicEndpoint).AssemblyQualifiedName,
                Message = message2,
                MessageType = typeof(TestEventOne).AssemblyQualifiedName,
                Headers = null,
                Sent = DateTime.UtcNow
            });
            _repository.Add(new OutboundMessageEntity
            {
                Endpoint = endpoint,
                EndpointType = typeof(BasicEndpoint).AssemblyQualifiedName,
                Message = message3,
                MessageType = typeof(TestEventOne).AssemblyQualifiedName,
                Headers = null
            });

            var worker = new OutboundMessagesWorker<OutboundMessageEntity>(_repository, _broker);
            worker.SendPendingMessages();

            var sentMessages = _broker.SentMessages;

            Assert.That(sentMessages.Count, Is.EqualTo(2));

            sentMessages.Clear();

            worker.SendPendingMessages();

            Assert.That(sentMessages.Count, Is.EqualTo(0));

            Assert.That(_repository.DbSet.All(m => m.Sent.HasValue));
        }
    }
}