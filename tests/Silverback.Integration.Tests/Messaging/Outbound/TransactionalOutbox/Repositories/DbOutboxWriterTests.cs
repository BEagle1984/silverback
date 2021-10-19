// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Tests.Integration.TestTypes.Database;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox.Repositories;

public sealed class DbOutboxWriterTests : IDisposable
{
    private readonly IOutboundEnvelope _sampleOutboundEnvelope = new OutboundEnvelope(
        new TestEventOne { Content = "Test" },
        new[] { new MessageHeader("one", "1"), new MessageHeader("two", "2") },
        TestProducerEndpoint.GetDefault())
    {
        RawMessage = BytesUtil.GetRandomStream()
    };

    private readonly SqliteConnection _connection;

    private readonly IServiceScope _scope;

    private readonly TestDbContext _dbContext;

    private readonly DbOutboxWriter _queueWriter;

    public DbOutboxWriterTests()
    {
        _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();

        ServiceCollection services = new();

        services
            .AddLoggerSubstitute()
            .AddDbContext<TestDbContext>(
                options => options
                    .UseSqlite(_connection.ConnectionString))
            .AddSilverback()
            .UseDbContext<TestDbContext>();

        ServiceProvider? serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true
            });

        _scope = serviceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
        _dbContext.Database.EnsureCreated();

        _queueWriter = new DbOutboxWriter(_scope.ServiceProvider.GetRequiredService<IDbContext>());
    }

    [Fact]
    public async Task WriteAsync_SomeMessages_TableStillEmpty()
    {
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);

        _dbContext.Outbox.Should().HaveCount(0);
    }

    [Fact]
    public async Task WriteAsyncAndSaveChanges_SomeMessages_MessagesAddedToQueue()
    {
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.CommitAsync();
        await _dbContext.SaveChangesAsync();

        _dbContext.Outbox.Should().HaveCount(3);
    }

    [Fact]
    public async Task WriteAsyncAndRollbackAsync_SomeMessages_TableStillEmpty()
    {
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.RollbackAsync();

        _dbContext.Outbox.Should().HaveCount(0);
    }

    [Fact]
    public async Task WriteAsyncCommitAndSaveChanges_Message_MessageCorrectlyAddedToQueue()
    {
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _queueWriter.CommitAsync();
        await _dbContext.SaveChangesAsync();

        OutboxMessage? outboundMessage = _dbContext.Outbox.First();
        outboundMessage.EndpointRawName.Should().Be("test");
        outboundMessage.MessageType.Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
        outboundMessage.Content.Should().NotBeNullOrEmpty();
        outboundMessage.Headers.Should().NotBeNullOrEmpty();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        _scope.Dispose();
    }
}
