// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Core.TestTypes.Behaviors
{
    public class TestSortedBehavior : IBehavior, ISorted
    {
        private readonly List<string> _calls;

        public TestSortedBehavior(int sortIndex, List<string> calls)
        {
            _calls = calls;
            SortIndex = sortIndex;
        }

        public int SortIndex { get; }

        public Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
        {
            _calls.Add(SortIndex.ToString());

            return next(messages);
        }
    }
}