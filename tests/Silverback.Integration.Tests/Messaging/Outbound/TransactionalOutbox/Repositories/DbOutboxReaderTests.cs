﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Tests.Integration.TestTypes.Database;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox.Repositories
{
    public sealed class DbOutboxReaderTests : IDisposable
    {
        // TestEventOne { Content = "Test" }
        private static readonly byte[] SampleContent =
        {
            0x7B, 0x22, 0x43, 0x6F, 0x6E, 0x74, 0x65, 0x6E, 0x74, 0x22, 0x3A, 0x22, 0x54, 0x65, 0x73, 0x74,
            0x22, 0x7D
        };

        // One=1, Two=2
        private static readonly byte[] SampleHeaders =
        {
            0x5B, 0x7B, 0x22, 0x4E, 0x61, 0x6D, 0x65, 0x22, 0x3A, 0x22, 0x6F, 0x6E, 0x65, 0x22, 0x2C, 0x22,
            0x56, 0x61,
            0x6C, 0x75, 0x65, 0x22, 0x3A, 0x22, 0x31, 0x22, 0x7D, 0x2C, 0x7B, 0x22, 0x4E, 0x61, 0x6D, 0x65,
            0x22, 0x3A,
            0x22, 0x74, 0x77, 0x6F, 0x22, 0x2C, 0x22, 0x56, 0x61, 0x6C, 0x75, 0x65, 0x22, 0x3A, 0x22, 0x32,
            0x22, 0x7D,
            0x5D
        };

        private readonly SqliteConnection _connection;

        private readonly IServiceScope _scope;

        private readonly TestDbContext _dbContext;

        private readonly DbOutboxReader _queueReader;

        public DbOutboxReaderTests()
        {
            _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
            _connection.Open();

            var services = new ServiceCollection();

            services
                .AddLoggerSubstitute()
                .AddDbContext<TestDbContext>(
                    options => options
                        .UseSqlite(_connection.ConnectionString))
                .AddSilverback()
                .UseDbContext<TestDbContext>();

            var serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            _scope = serviceProvider.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
            _dbContext.Database.EnsureCreated();

            _queueReader = new DbOutboxReader(_scope.ServiceProvider.GetRequiredService<IDbContext>());
        }

        [Fact]
        public async Task GetMaxAge_SomeMessagesInQueue_MaxAgeReturned()
        {
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-30),
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-20),
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-60),
                    EndpointName = "test-topic"
                });
            await _dbContext.SaveChangesAsync();

            var maxAge = await _queueReader.GetMaxAgeAsync();

            maxAge.Should().BeGreaterOrEqualTo(TimeSpan.FromSeconds(60));
        }

        [Fact]
        public async Task GetMaxAge_EmptyQueue_ZeroReturned()
        {
            var maxAge = await _queueReader.GetMaxAgeAsync();

            maxAge.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public async Task Dequeue_SomeMessages_MessagesReturned()
        {
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-30),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-20),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-60),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            await _dbContext.SaveChangesAsync();

            var messages = await _queueReader.ReadAsync(5);

            messages.Should().NotBeNull();
            messages.Should().HaveCount(3);
        }

        [Fact]
        public async Task Dequeue_Message_HeadersDeserialized()
        {
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-30),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            await _dbContext.SaveChangesAsync();

            var message = (await _queueReader.ReadAsync(1)).First();

            message.Headers.Should().BeEquivalentTo(
                new[]
                {
                    new MessageHeader("one", "1"),
                    new MessageHeader("two", "2")
                });
        }

        [Fact]
        public async Task Dequeue_MessageWithLegacyHeadersSerialization_HeadersDeserialized()
        {
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-30),
                    Content = SampleContent,
#pragma warning disable CS0618 // Obsolete
                    Headers = "[{\"Name\":\"one\",\"Value\":\"1\"},{\"Name\":\"two\",\"Value\":\"2\"}]",
#pragma warning restore CS0618 // Obsolete
                    EndpointName = "test-topic"
                });
            await _dbContext.SaveChangesAsync();

            var message = (await _queueReader.ReadAsync(1)).First();

            message.Headers.Should().BeEquivalentTo(
                new[]
                {
                    new MessageHeader("one", "1"),
                    new MessageHeader("two", "2")
                });
        }

        [Fact]
        public async Task Retry_DequeuedSomeMessages_MessagesStillInQueue()
        {
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-30),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-20),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-60),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            await _dbContext.SaveChangesAsync();

            var messages = await _queueReader.ReadAsync(2);

            foreach (var message in messages)
            {
                await _queueReader.RetryAsync(message);
            }

            _dbContext.Outbox.Should().HaveCount(3);
        }

        [Fact]
        public async Task Acknowledge_DequeuedSomeMessages_MessagesRemoved()
        {
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-30),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-20),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-60),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            await _dbContext.SaveChangesAsync();

            var messages = await _queueReader.ReadAsync(2);

            foreach (var message in messages)
            {
                await _queueReader.AcknowledgeAsync(message);
            }

            _dbContext.Outbox.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetLength_SomeMessagesInQueue_CountReturned()
        {
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-30),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-20),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            _dbContext.Outbox.Add(
                new OutboxMessage
                {
                    Created = DateTime.UtcNow.AddSeconds(-60),
                    Content = SampleContent,
                    SerializedHeaders = SampleHeaders,
                    EndpointName = "test-topic"
                });
            await _dbContext.SaveChangesAsync();

            int length = await _queueReader.GetLengthAsync();

            length.Should().Be(3);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
            _scope.Dispose();
        }
    }
}
