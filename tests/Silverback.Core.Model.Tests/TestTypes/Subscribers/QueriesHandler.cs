// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.Model.TestTypes.Messages;

namespace Silverback.Tests.Core.Model.TestTypes.Subscribers
{
    public class QueriesHandler : ISubscriber
    {
        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "CA1822", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public Task<IEnumerable<int>> Handle(ListQuery query) => Task.FromResult(Enumerable.Range(1, query.Count));

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "CA1822", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        public Task TryToBreak(ListQuery query) => Task.FromResult(Array.Empty<object>());
    }
}
