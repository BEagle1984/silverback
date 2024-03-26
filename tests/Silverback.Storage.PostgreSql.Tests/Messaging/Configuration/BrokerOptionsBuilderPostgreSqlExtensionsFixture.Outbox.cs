// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Messaging.Configuration;

public partial class BrokerOptionsBuilderPostgreSqlExtensionsFixture
{
    [Fact]
    public void AddPostgreSqlOutbox_ShouldConfigureOutboxFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddPostgreSqlOutbox()));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        OutboxSettings outboxSettings = new PostgreSqlOutboxSettings("conn");

        IOutboxReader reader = readerFactory.GetReader(outboxSettings, serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(outboxSettings, serviceProvider);

        reader.Should().BeOfType<PostgreSqlOutboxReader>();
        writer.Should().BeOfType<PostgreSqlOutboxWriter>();
    }
}
