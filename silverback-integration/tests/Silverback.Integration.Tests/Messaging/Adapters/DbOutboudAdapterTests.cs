using System;
using System.Linq;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Adapters
{
    [TestFixture]
    public class DbOutboudAdapterTests
    {
        private OutboundMessagesRepository _repository;

        [SetUp]
        public void Setup()
        {
            _repository = new OutboundMessagesRepository();
        }

        [Test]
        public void RelayTest()
        {
            var adapter = new DbOutboundAdapter<OutboundMessageEntity>(_repository);

            adapter.Relay(new TestEventOne { Content = "Test" }, null, BasicEndpoint.Create("TestEventOneTopic"));

            Assert.That(_repository.DbSet.Count, Is.EqualTo(1));
            var entity = _repository.DbSet.First();
            Assert.That(entity.Endpoint, Is.EqualTo("{\"Name\":\"TestEventOneTopic\"}"));
            Assert.That(entity.EndpointType.StartsWith("Silverback.Messaging.BasicEndpoint, Silverback.Integration, Version="));
            Assert.That(entity.Message.StartsWith("{\"Content\":\"Test\""));
            Assert.That(entity.MessageType.StartsWith("Silverback.Tests.TestTypes.Domain.TestEventOne, Silverback.Integration.Tests, Version="));
            Assert.That(entity.MessageId, Is.Not.EqualTo(Guid.Empty));
        }
    }
}
