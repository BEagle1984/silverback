﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event fired when an exception occurs during the processing of a (DB) transaction.
    ///     It is fired by the data access while saving changes (see Silverback.Core.EntityFrameworkCore)
    ///     and it is internally used (in Silverback.Integration) to trigger additional tasks related to the
    ///     publishing of the domain events.
    /// </summary>
    public class TransactionAbortedEvent : ISilverbackEvent
    {
    }
}