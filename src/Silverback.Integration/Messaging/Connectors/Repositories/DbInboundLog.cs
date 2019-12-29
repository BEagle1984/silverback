// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Infrastructure;
using Silverback.Messaging.Messages;
using InboundMessage = Silverback.Messaging.Connectors.Model.InboundMessage;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbInboundLog : RepositoryBase<InboundMessage>, IInboundLog
    {
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public DbInboundLog(IDbContext dbContext, MessageKeyProvider messageKeyProvider) : base(dbContext)
        {
            _messageKeyProvider = messageKeyProvider;
        }

        public async Task Add(object message, IConsumerEndpoint endpoint)
        {
            await _semaphore.WaitAsync();
            
            try
            {
                DbSet.Add(new InboundMessage
                {
                    MessageId = _messageKeyProvider.GetKey(message),
                    Message = DefaultSerializer.Serialize(message),
                    EndpointName = endpoint.Name,
                    Consumed = DateTime.UtcNow
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task Commit()
        {
            await _semaphore.WaitAsync();

            try
            {
                // Call SaveChanges, in case it isn't called by a subscriber
                await DbContext.SaveChangesAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task Rollback()
        {
            // Nothing to do, just not saving the changes made to the DbContext
            return Task.CompletedTask;
        }

        public Task<bool> Exists(object message, IConsumerEndpoint endpoint)
        {
            var key = _messageKeyProvider.GetKey(message);
            return DbSet.AsQueryable().AnyAsync(m => m.MessageId == key && m.EndpointName == endpoint.Name);
        }

        public Task<int> GetLength() => DbSet.AsQueryable().CountAsync();
    }
}