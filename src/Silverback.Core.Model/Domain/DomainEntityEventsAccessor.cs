// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Domain
{
    public static class DomainEntityEventsAccessor
    {
        public static Func<IDomainEntity, IEnumerable<object>> EventsSelector = e => e.DomainEvents;

        public static Action<IDomainEntity> ClearEventsAction = e => e.ClearEvents();
    }
}
