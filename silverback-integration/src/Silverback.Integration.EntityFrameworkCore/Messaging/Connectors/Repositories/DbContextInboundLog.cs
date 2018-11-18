using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbContextInboundLog : RepositoryBase<InboundMessage>, IInboundLog
    {
        public DbContextInboundLog(DbContext dbContext) : base(dbContext)
        {
        }

        public void Add(IIntegrationMessage message, IEndpoint endpoint)
        {
            throw new System.NotImplementedException();
        }

        public void Commit()
        {
            // Call SaveChanges just in case (it is likely been called already by a subscriber)
            DbContext.SaveChanges();
        }

        public void Rollback()
        {
            // Nothing to do, just not saving the 
        }

        public bool Exists(IIntegrationMessage message, IEndpoint endpoint) =>
            DbSet.Any(m => m.MessageId == message.Id && m.EndpointName == endpoint.Name);

        public int Length => DbSet.Count();
    }
}