// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Reads from the EntityFramework outbox.
/// </summary>
public class EntityFrameworkOutboxReader : IOutboxReader
{
    private readonly EntityFrameworkOutboxSettings _settings;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkOutboxReader" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    /// <param name="serviceScopeFactory">
    ///     The <see cref="IServiceScopeFactory" />.
    /// </param>
    public EntityFrameworkOutboxReader(EntityFrameworkOutboxSettings settings, IServiceScopeFactory serviceScopeFactory)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
    }

    /// <inheritdoc cref="IOutboxReader.GetAsync" />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed in the returned enumerable")]
    public async Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count)
    {
        IServiceScope scope = _serviceScopeFactory.CreateScope();
        DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        try
        {
            IAsyncEnumerable<DbOutboxMessage> asyncEnumerable = dbContext.Set<SilverbackOutboxMessage>()
                .AsNoTracking()
                .OrderBy(message => message.Created)
                .Take(count)
                .AsAsyncEnumerable()
                .Select(
                    message => new DbOutboxMessage(
                        message.Id,
                        message.Content,
                        message.Headers == null ? null : JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(message.Headers),
                        message.EndpointName));

            return new DbContextAsyncEnumerable<OutboxMessage>(asyncEnumerable, dbContext, scope);
        }
        catch
        {
            await dbContext.DisposeAsync().ConfigureAwait(false);
            scope.Dispose();
            throw;
        }
    }

    /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
    public async Task<int> GetLengthAsync()
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        return await dbContext.Set<SilverbackOutboxMessage>()
            .AsNoTracking()
            .CountAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
    public async Task<TimeSpan> GetMaxAgeAsync()
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        DateTime oldestCreated = await dbContext.Set<SilverbackOutboxMessage>()
            .AsNoTracking()
            .DefaultIfEmpty()
            .MinAsync(message => message != null ? message.Created : DateTime.MinValue)
            .ConfigureAwait(true);

        if (oldestCreated == default)
            return TimeSpan.Zero;

        return DateTime.UtcNow - oldestCreated;
    }

    /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync" />
    public async Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        long[] identifiers = outboxMessages.Cast<DbOutboxMessage>().Select(message => message.Id).ToArray();

        await dbContext.Set<SilverbackOutboxMessage>()
            .Where(message => identifiers.Contains(message.Id))
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);
    }
}
