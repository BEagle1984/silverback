// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Core.TestTypes.Behaviors
{
    public class TestBehavior : IBehavior
    {
        private readonly IList<string>? _calls;

        public TestBehavior(IList<string>? calls = null)
        {
            _calls = calls;
        }

        public int EnterCount { get; private set; }

        public int ExitCount { get; private set; }

        public Task<IReadOnlyCollection<object?>> HandleAsync(
            object message,
            MessageHandler next,
            CancellationToken cancellationToken = default)
        {
            _calls?.Add("unsorted");

            EnterCount++;

            var result = next(message, cancellationToken);

            ExitCount++;

            return result;
        }
    }
}
