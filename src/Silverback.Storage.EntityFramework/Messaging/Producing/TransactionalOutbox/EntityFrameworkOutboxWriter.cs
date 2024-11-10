// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Writes to the EntityFramework outbox.
/// </summary>
public class EntityFrameworkOutboxWriter : IOutboxWriter
{
    private readonly EntityFrameworkOutboxSettings _settings;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkOutboxWriter" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    /// <param name="serviceScopeFactory">
    ///     The <see cref="IServiceScopeFactory" />.
    /// </param>
    public EntityFrameworkOutboxWriter(EntityFrameworkOutboxSettings settings, IServiceScopeFactory serviceScopeFactory)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
    }

    /// <inheritdoc cref="AddAsync(OutboxMessage,ISilverbackContext?,CancellationToken)" />
    public Task AddAsync(
        OutboxMessage outboxMessage,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default) =>
        AddAsync(Enumerable.Repeat(outboxMessage, 1), context, cancellationToken);

    /// <inheritdoc cref="AddAsync(IEnumerable{OutboxMessage},ISilverbackContext?,CancellationToken)" />
    public async Task AddAsync(
        IEnumerable<OutboxMessage> outboxMessages,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider, context);
        await using ConfiguredAsyncDisposable disposable = dbContext.ConfigureAwait(false);

        dbContext.UseTransactionIfAvailable(context);

        foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            dbContext.Add(MapToEntity(outboxMessage));
        }

        dbContext.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="AddAsync(IAsyncEnumerable{OutboxMessage},ISilverbackContext?,CancellationToken)" />
    public async Task AddAsync(
        IAsyncEnumerable<OutboxMessage> outboxMessages,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider, context);
        await using ConfiguredAsyncDisposable disposable = dbContext.ConfigureAwait(false);

        dbContext.UseTransactionIfAvailable(context);

        await foreach (OutboxMessage outboxMessage in outboxMessages.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            dbContext.Add(MapToEntity(outboxMessage));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static SilverbackOutboxMessage MapToEntity(OutboxMessage outboxMessage) =>
        new()
        {
            Content = outboxMessage.Content,
            Headers = outboxMessage.Headers == null ? null : JsonSerializer.Serialize(outboxMessage.Headers),
            EndpointName = outboxMessage.EndpointName,
            Created = DateTime.UtcNow
        };
}
