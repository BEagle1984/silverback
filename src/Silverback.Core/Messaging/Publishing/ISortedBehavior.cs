// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Publishing
{
    public interface ISortedBehavior : IBehavior
    {
        int SortIndex { get; }
    }
}