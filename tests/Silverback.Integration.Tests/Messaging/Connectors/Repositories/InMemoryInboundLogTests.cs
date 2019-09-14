// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
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
        public async Task AddTest()
        {
            await _log.Add(new TestEventOne(), TestEndpoint.Default);
            await _log.Add(new TestEventOne(), TestEndpoint.Default);
            await _log.Add(new TestEventOne(), TestEndpoint.Default);

            (await _log.GetLength()).Should().Be(0);
        }

        [Fact]
        public async Task CommitTest()
        {
            await _log.Add(new TestEventOne(), TestEndpoint.Default);
            await _log.Add(new TestEventOne(), TestEndpoint.Default);
            await _log.Add(new TestEventOne(), TestEndpoint.Default);

            await _log.Commit();

            (await _log.GetLength()).Should().Be(3);
        }

        [Fact]
        public async Task RollbackTest()
        {
            await _log.Add(new TestEventOne(), TestEndpoint.Default);
            await _log.Add(new TestEventOne(), TestEndpoint.Default);
            await _log.Add(new TestEventOne(), TestEndpoint.Default);

            await _log.Rollback();

            (await _log.GetLength()).Should().Be(0);
        }

        [Fact]
        public async Task ExistsPositiveTest()
        {
            var messageId = Guid.NewGuid();
            await _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            await _log.Add(new TestEventOne { Id = messageId }, TestEndpoint.Default);
            await _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            await _log.Commit();

            var result = await _log.Exists(new TestEventOne{Id = messageId}, TestEndpoint.Default);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsNegativeTest()
        {
            await _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            await _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);
            await _log.Add(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);

            var result = await _log.Exists(new TestEventOne { Id = Guid.NewGuid() }, TestEndpoint.Default);

            result.Should().BeFalse();
        }
    }
}