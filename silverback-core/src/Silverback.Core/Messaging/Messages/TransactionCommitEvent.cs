namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The event fired when a (DB) transaction is committed.
    /// It is fired by the data access after saving changes (see Silverback.Core.EntityFrameworkCore)
    /// and it is internally used (in Silverback.Integration) to trigger additional tasks related to the
    /// publishing of the domain events. 
    /// </summary>
    public class TransactionCommitEvent : IEvent
    {
    }
}