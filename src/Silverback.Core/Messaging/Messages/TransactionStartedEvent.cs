// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     The event published when a (database) transaction is being started. It is fired by the data access
///     before saving changes (see Silverback.Core.EntityFrameworkCore) and it is internally used (in
///     Silverback.Integration) to trigger additional tasks related to the publishing of the domain events.
/// </summary>
public class TransactionStartedEvent : ISilverbackEvent
{
}
