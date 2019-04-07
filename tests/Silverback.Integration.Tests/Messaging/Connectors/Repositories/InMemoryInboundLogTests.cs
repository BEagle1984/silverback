// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Repositories
{
    [Collection("StaticInMemory")]
    public class InMemoryInboundLogTests
    {
        private readonly InMemoryInboundLog _log;

        public InMemoryInboundLogTests()
        {
            _log = new InMemoryInboundLog(new MessageKeyProvider(new []{new DefaultPropertiesMessageKeyProvider()}));
            InMemoryInboundLog.Clear();
        }

        [Fact]
        public void AddTest()
        {
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);

            _log.Length.Should().Be(0);
        }

        [Fact]
        public void CommitTest()
        {
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);

            _log.Commit();

            _log.Length.Should().Be(3);
        }

        [Fact]
        public void RollbackTest()
        {
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);
            _log.Add(new TestEventOne(), TestEndpoint.Default);

            _log.Rollback();

            _log.Length.Should().Be(0);
        }

        [Fact]
        public void ExistsPositiveTest()
        {
            var messageId = Guid.NewGuid();
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            _log.Add(new TestEventOne { Id = messageId }, TestEndpoint.Default);
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            _log.Commit();

            var result = _log.Exists(new TestEventOne{Id = messageId}, TestEndpoint.Default);

            result.Should().BeTrue();
        }

        [Fact]
        public void ExistsNegativeTest()
        {
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);

            var result = _log.Exists(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);

            result.Should().BeFalse();
        }
    }
}