// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NUnit.Framework;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Connectors.Repositories
{
    [TestFixture]
    public class InMemoryInboundLogTests
    {
        private InMemoryInboundLog _log;

        [SetUp]
        public void Setup()
        {
            _log = new InMemoryInboundLog();
            InMemoryInboundLog.Clear();
        }

        [Test]
        public void AddTest()
        {
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);

            Assert.That(_log.Length, Is.EqualTo(0));
        }

        [Test]
        public void CommitTest()
        {
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);

            _log.Commit();

            Assert.That(_log.Length, Is.EqualTo(3));
        }

        [Test]
        public void RollbackTest()
        {
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);

            _log.Rollback();

            Assert.That(_log.Length, Is.EqualTo(0));
        }

        [Test]
        public void ExistsPositiveTest()
        {
            var messageId = Guid.NewGuid();
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            _log.Add(new TestEventOne { Id = messageId }, TestEndpoint.Default);
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            _log.Commit();

            var result = _log.Exists(new TestEventOne{Id = messageId}, TestEndpoint.Default);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ExistsNegativeTest()
        {
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);

            var result = _log.Exists(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);

            Assert.That(result, Is.False);
        }
    }
}