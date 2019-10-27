// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The event fired when a (DB) transaction is completed and committed.
    /// It is fired by the data access after saving changes (see Silverback.Core.EntityFrameworkCore)
    /// and it is internally used (in Silverback.Integration) to trigger additional tasks related to the
    /// publishing of the domain events. 
    /// </summary>
    public class TransactionCompletedEvent : ISilverbackEvent
    {
    }
}