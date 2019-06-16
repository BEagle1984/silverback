// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The event fired when a (DB) transaction is being started.
    /// It is fired by the data access before saving changes (see Silverback.Core.EntityFrameworkCore)
    /// and it is internally used (in Silverback.Integration) to trigger additional tasks related to the
    /// publishing of the domain events. 
    /// </summary>
    public class TransactionStartedEvent : ISilverbackEvent
    {
    }
}