// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public class BehaviorsProviderTests
{
    [Fact]
    public void CreateStack_ShouldReturnBehaviorsInSortOrder()
    {
        SortedBehavior behavior1 = new(100);
        SortedBehavior behavior2 = new(-100);
        UnsortedBehavior behavior3 = new();
        SortedBehavior behavior4 = new(0);
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(behavior1)
            .AddSingletonBehavior(behavior2)
            .AddSingletonBehavior(behavior3)
            .AddScopedBehavior(_ => behavior4));
        BehaviorsProvider behaviorsProvider = new(serviceProvider);

        Stack<IBehavior> stack = behaviorsProvider.CreateStack();

        stack.ShouldBe([behavior2, behavior4, behavior3, behavior1]);
    }

    [Fact]
    public void CreateStack_ShouldReturnNewStack()
    {
        SortedBehavior behavior1 = new(100);
        SortedBehavior behavior2 = new(-100);
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(behavior1)
            .AddSingletonBehavior(behavior2));
        BehaviorsProvider behaviorsProvider = new(serviceProvider);

        Stack<IBehavior> stack1 = behaviorsProvider.CreateStack();
        stack1.Pop();
        Stack<IBehavior> stack2 = behaviorsProvider.CreateStack();

        stack1.ShouldBe([behavior1]);
        stack2.ShouldBe([behavior2, behavior1]);
    }

    private class UnsortedBehavior : IBehavior
    {
        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken) =>
            next(message);
    }

    private sealed class SortedBehavior : UnsortedBehavior, ISorted
    {
        public SortedBehavior(int sortIndex)
        {
            SortIndex = sortIndex;
        }

        public int SortIndex { get; }
    }
}
