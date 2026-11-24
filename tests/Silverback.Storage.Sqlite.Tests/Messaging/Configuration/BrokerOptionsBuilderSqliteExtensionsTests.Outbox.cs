// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Configuration;

public partial class BrokerOptionsBuilderSqliteExtensionsTests
{
    [Fact]
    public void AddSqliteOutbox_ShouldConfigureOutboxFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddSqliteOutbox()));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        OutboxSettings outboxSettings = new SqliteOutboxSettings("conn");

        IOutboxReader reader = readerFactory.GetReader(outboxSettings, serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(outboxSettings, serviceProvider);

        reader.ShouldBeOfType<SqliteOutboxReader>();
        writer.ShouldBeOfType<SqliteOutboxWriter>();
    }

    [Fact]
    public void UseSqliteOutbox_ShouldOverrideAllOutboxSettingsTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.UseSqliteOutbox("conn")));

        OutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<OutboxReaderFactory>();
        OutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<OutboxWriterFactory>();

        readerFactory.AddFactory<OutboxSettings1>((_, _) => new OutboxReader1());
        readerFactory.AddFactory<OutboxSettings2>((_, _) => new OutboxReader2());
        writerFactory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());
        writerFactory.AddFactory<OutboxSettings2>((_, _) => new OutboxWriter2());

        IOutboxReader reader1 = readerFactory.GetReader(new OutboxSettings1(), serviceProvider);
        IOutboxReader reader2 = readerFactory.GetReader(new OutboxSettings2(), serviceProvider);
        IOutboxWriter writer1 = writerFactory.GetWriter(new OutboxSettings1(), serviceProvider);
        IOutboxWriter writer2 = writerFactory.GetWriter(new OutboxSettings2(), serviceProvider);

        reader1.ShouldBeOfType<SqliteOutboxReader>();
        reader2.ShouldBeOfType<SqliteOutboxReader>();
        writer1.ShouldBeOfType<SqliteOutboxWriter>();
        writer2.ShouldBeOfType<SqliteOutboxWriter>();
    }

    [Fact]
    public void AddSqliteOutbox_ShouldNotThrow_WhenCalledTwice()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddSqliteOutbox().AddSqliteOutbox()));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        OutboxSettings outboxSettings = new SqliteOutboxSettings("conn");

        readerFactory.GetReader(outboxSettings, serviceProvider).ShouldBeOfType<SqliteOutboxReader>();
        writerFactory.GetWriter(outboxSettings, serviceProvider).ShouldBeOfType<SqliteOutboxWriter>();
    }

    [Fact]
    public void AddSqliteOutbox_ShouldThrow_WhenWithConnectionToMessageBrokerNotCalled()
    {
        BrokerOptionsBuilder builder = new(new ServiceCollection().AddSilverback());

        Action act = () => builder.AddSqliteOutbox();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("WithConnectionToMessageBroker has not been called");
    }

    [Fact]
    public void AddSqliteOutbox_ShouldThrow_WhenOutboxWriterFactoryNotRegistered()
    {
        ServiceCollection services = [];
        services.AddSingleton(new OutboxReaderFactory());
        BrokerOptionsBuilder builder = new(services.AddSilverback());

        Action act = () => builder.AddSqliteOutbox();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("WithConnectionToMessageBroker has not been called");
    }

    [Fact]
    public void UseSqliteOutbox_ShouldThrow_WhenWithConnectionToMessageBrokerNotCalled()
    {
        BrokerOptionsBuilder builder = new(new ServiceCollection().AddSilverback());

        Action act = () => builder.UseSqliteOutbox("conn");

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("WithConnectionToMessageBroker has not been called");
    }

    [Fact]
    public void UseSqliteOutbox_ShouldUseSpecifiedSettings_WhenSettingsAreSqliteOutboxSettings()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.UseSqliteOutbox("conn")));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        SqliteOutboxSettings outboxSettings = new("other-conn")
        {
            TableName = "other-table"
        };

        IOutboxReader reader = readerFactory.GetReader(outboxSettings, serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(outboxSettings, serviceProvider);

        GetSettings(reader.ShouldBeOfType<SqliteOutboxReader>()).ShouldBeSameAs(outboxSettings);
        GetSettings(writer.ShouldBeOfType<SqliteOutboxWriter>()).ShouldBeSameAs(outboxSettings);
    }

    [Fact]
    public void UseSqliteOutbox_ShouldUseDefaultSettings_WhenSettingsAreNotDatabaseConnectionSettings()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.UseSqliteOutbox("conn")));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        IOutboxReader reader = readerFactory.GetReader(new OutboxSettings1(), serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(new OutboxSettings1(), serviceProvider);

        GetSettings(reader.ShouldBeOfType<SqliteOutboxReader>()).ShouldBe(new SqliteOutboxSettings("conn"));
        GetSettings(writer.ShouldBeOfType<SqliteOutboxWriter>()).ShouldBe(new SqliteOutboxSettings("conn"));
    }

    [Fact]
    public void UseSqliteOutbox_ShouldMapTimeouts_WhenSettingsAreDatabaseConnectionSettings()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.UseSqliteOutbox("conn")));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        DatabaseOutboxSettings outboxSettings = new()
        {
            DbCommandTimeout = TimeSpan.FromSeconds(42),
            CreateTableTimeout = TimeSpan.FromSeconds(84)
        };

        IOutboxReader reader = readerFactory.GetReader(outboxSettings, serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(outboxSettings, serviceProvider);

        SqliteOutboxSettings readerSettings = GetSettings(reader.ShouldBeOfType<SqliteOutboxReader>()).ShouldBeOfType<SqliteOutboxSettings>();
        readerSettings.ConnectionString.ShouldBe("conn");
        readerSettings.DbCommandTimeout.ShouldBe(TimeSpan.FromSeconds(42));
        readerSettings.CreateTableTimeout.ShouldBe(TimeSpan.FromSeconds(84));
        SqliteOutboxSettings writerSettings = GetSettings(writer.ShouldBeOfType<SqliteOutboxWriter>()).ShouldBeOfType<SqliteOutboxSettings>();
        writerSettings.ConnectionString.ShouldBe("conn");
        writerSettings.DbCommandTimeout.ShouldBe(TimeSpan.FromSeconds(42));
        writerSettings.CreateTableTimeout.ShouldBe(TimeSpan.FromSeconds(84));
    }

    private static object? GetSettings(object instance) =>
        instance.GetType().GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic).ShouldNotBeNull().GetValue(instance);

    private record DatabaseOutboxSettings : OutboxSettings, IDatabaseConnectionSettings
    {
        public string ConnectionString => "other-conn";

        public string TableName { get; init; } = "other-table";

        public TimeSpan DbCommandTimeout { get; init; }

        public TimeSpan CreateTableTimeout { get; init; }

        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }

    private record OutboxSettings1 : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }

    private record OutboxSettings2 : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }

    private class OutboxReader1 : IOutboxReader
    {
        public Task<int> GetLengthAsync() => throw new NotSupportedException();

        public Task<TimeSpan> GetMaxAgeAsync() => throw new NotSupportedException();

        public Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count) => throw new NotSupportedException();

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => throw new NotSupportedException();
    }

    private class OutboxReader2 : IOutboxReader
    {
        public Task<int> GetLengthAsync() => throw new NotSupportedException();

        public Task<TimeSpan> GetMaxAgeAsync() => throw new NotSupportedException();

        public Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count) => throw new NotSupportedException();

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => throw new NotSupportedException();
    }

    private class OutboxWriter1 : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private class OutboxWriter2 : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
