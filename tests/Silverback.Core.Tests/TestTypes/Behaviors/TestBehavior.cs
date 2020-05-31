// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Core.TestTypes.Behaviors
{
    public class TestBehavior : IBehavior
    {
        private readonly List<string>? _calls;

        public TestBehavior(List<string>? calls = null)
        {
            _calls = calls;
        }

        public int EnterCount { get; private set; }

        public int ExitCount { get; private set; }

        public Task<IReadOnlyCollection<object>> Handle(IReadOnlyCollection<object> messages, MessagesHandler next)
        {
            _calls?.Add("unsorted");

            EnterCount++;

            var result = next(messages);

            ExitCount++;

            return result;
        }
    }
}
