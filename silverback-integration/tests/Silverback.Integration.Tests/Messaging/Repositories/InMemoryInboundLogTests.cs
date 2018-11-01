using System;
using System.Threading;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Repositories;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Repositories
{
    [TestFixture]
    public class InMemoryInboundLogTests
    {
        private InMemoryInboundLog _log;

         [SetUp]
         public void Setup()
         {
             _log = new InMemoryInboundLog();
             _log.ClearOlderEntries(DateTime.Now);
         }

        [Test]
        public void AddTest()
        {
            _log.Add(new TestEventOne(), BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne(), BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne(), BasicEndpoint.Create("test"));

            Assert.That(_log.Count, Is.EqualTo(3));
        }

        [Test]
        public void ExistsPositiveTest()
        {
            var messageId = Guid.NewGuid();
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne { Id = messageId }, BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));

            var result = _log.Exists(new TestEventOne{Id = messageId}, BasicEndpoint.Create("test"));

            Assert.That(result, Is.True);
        }

        [Test]
        public void ExistsNegativeTest()
        {
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));

            var result = _log.Exists(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));

            Assert.That(result, Is.False);
        }

        [Test]
        public void CleanupTest()
        {
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));

            var threshold = DateTime.Now;
            Thread.Sleep(100);

            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, BasicEndpoint.Create("test"));

            _log.ClearOlderEntries(threshold);

            Assert.That(_log.Count, Is.EqualTo(2));
        }
    }
}