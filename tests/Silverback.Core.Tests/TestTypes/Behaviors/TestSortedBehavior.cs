// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Core.TestTypes.Behaviors;

public class TestSortedBehavior : IBehavior, ISorted
{
    private readonly IList<string> _calls;

    public TestSortedBehavior(int sortIndex, IList<string> calls)
    {
        _calls = calls;
        SortIndex = sortIndex;
    }

    public int SortIndex { get; }

    public Task<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next)
    {
        _calls.Add(SortIndex.ToString(CultureInfo.InvariantCulture));

        return next(message);
    }
}
