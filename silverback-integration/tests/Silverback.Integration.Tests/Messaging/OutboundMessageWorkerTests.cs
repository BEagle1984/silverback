using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class OutboundMessageWorkerTests
    {
        private OutboundMessagesRepository _repository;

        [SetUp]
        public void Setup()
        {
            BrokersConfig.Instance.Clear();
            BrokersConfig.Instance.Add<FakeBroker>(c => c.UseServer("server"));

            _repository = new OutboundMessagesRepository();
        }

        // TODO: Test mismatching type versions

        [Test]
        public void SendPendingMessagesOnlyTest()
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

            var worker = new OutboundMessagesWorker<OutboundMessageEntity>(_repository);
            worker.SendPendingMessages();

            var sentMessages = BrokersConfig.Instance.GetDefault<FakeBroker>().SentMessages;

            Assert.That(sentMessages.Count, Is.EqualTo(2));

            var serializer = BrokersConfig.Instance.GetDefault<FakeBroker>().GetSerializer();
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

            var worker = new OutboundMessagesWorker<OutboundMessageEntity>(_repository);
            worker.SendPendingMessages();

            var sentMessages = BrokersConfig.Instance.GetDefault<FakeBroker>().SentMessages;
            var serializer = BrokersConfig.Instance.GetDefault<FakeBroker>().GetSerializer();
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

            var worker = new OutboundMessagesWorker<OutboundMessageEntity>(_repository);
            worker.SendPendingMessages();

            var sentMessages = BrokersConfig.Instance.GetDefault<FakeBroker>().SentMessages;

            Assert.That(sentMessages.Count, Is.EqualTo(2));

            sentMessages.Clear();

            worker.SendPendingMessages();

            Assert.That(sentMessages.Count, Is.EqualTo(0));

            Assert.That(_repository.DbSet.All(m => m.Sent.HasValue));
        }
    }
}