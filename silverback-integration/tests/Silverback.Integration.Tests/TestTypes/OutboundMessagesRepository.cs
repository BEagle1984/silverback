using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Repositories;

namespace Silverback.Tests.TestTypes
{
    // TODO 1

    //public class OutboundMessagesRepository : IOutboundMessagesRepository<OutboundMessageEntity>
    //{
    //    public HashSet<OutboundMessageEntity> DbSet { get; } = new HashSet<OutboundMessageEntity>();

    //    public OutboundMessageEntity Create()
    //        => new OutboundMessageEntity();

    //    public void Add(OutboundMessageEntity entity)
    //        => DbSet.Add(entity);

    //    public IEnumerable<OutboundMessageEntity> GetPending()
    //        => DbSet.Where(m => m.Sent == null).ToList();

    //    public void SaveChanges()
    //    {
    //        // Nothing to do
    //    }
    //}
}