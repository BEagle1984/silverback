// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.Model.TestTypes.Messages;

namespace Silverback.Tests.Core.Model.TestTypes.Subscribers
{
    public class QueriesHandler : ISubscriber
    {
        [Subscribe]
        public Task<IEnumerable<int>> Handle(ListQuery query) => Task.FromResult(Enumerable.Range(1, query.Count));

        [Subscribe]
        public Task TryToBreak(ListQuery query) => Task.FromResult(new object[0]);
    }
}