// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class MergeableActionCollectionFixture
{
    [Fact]
    public void AddOrAppend_ShouldAddNewAction()
    {
        int call1 = 0, call2 = 0;
        void Action1(int number) => call1 += number;
        void Action2(int number) => call2 += number;

        MergeableActionCollection<int> collection = new();

        collection.AddOrAppend("first", Action1);
        collection.AddOrAppend("second", Action2);

        collection.Should().HaveCount(2);
        collection.Select(action => action.Action).Should().BeEquivalentTo(new[] { Action1, Action2 });
        collection.First().Action.Invoke(1);
        collection.Skip(1).First().Action.Invoke(2);
        call1.Should().Be(1);
        call2.Should().Be(2);
    }

    [Fact]
    public void AddOrAppend_ShouldAppendToExistingAction()
    {
        int call1 = 0, call2 = 0;
        void Action1(int number) => call1 += number;
        void Action2(int number) => call2 += number;

        MergeableActionCollection<int> collection = new();

        collection.AddOrAppend("test", Action1);
        collection.AddOrAppend("test", Action2);

        collection.Should().HaveCount(1);
        collection.First().Action.Invoke(42);
        call1.Should().Be(42);
        call2.Should().Be(42);
    }

    [Fact]
    public void PrependToAll_ShouldPrependToAllExistingAction()
    {
        int call1 = 0, call2 = 0, call0 = 0;
        void Action1(int number) => call1 += number;
        void Action2(int number) => call2 += number;
        void Action0(int number) => call0 += number;

        MergeableActionCollection<int> collection = new();
        collection.AddOrAppend("test1", Action1);
        collection.AddOrAppend("test2", Action2);

        collection.PrependToAll(Action0);

        collection.First().Action.Invoke(42);
        collection.Skip(1).First().Action.Invoke(13);
        call1.Should().Be(42);
        call2.Should().Be(13);
        call0.Should().Be(55);
    }

    [Fact]
    public void Append_ShouldMergeCollections()
    {
        int call1 = 0, call2 = 0, call3 = 0, call4 = 0;
        void Action1(int number) => call1 += number;
        void Action2(int number) => call2 += number;
        void Action3(int number) => call3 += number;
        void Action4(int number) => call4 += number;

        MergeableActionCollection<int> collection1 = new();
        collection1.AddOrAppend("test1", Action1);
        collection1.AddOrAppend("test2", Action2);

        MergeableActionCollection<int> collection2 = new();
        collection2.AddOrAppend("test1", Action3);
        collection2.AddOrAppend("test2", Action4);

        collection1.Append(collection2);

        collection1.Should().HaveCount(2);
        collection2.Should().HaveCount(2);

        collection1.First().Action.Invoke(42);
        collection1.Skip(1).First().Action.Invoke(13);
        call1.Should().Be(42);
        call2.Should().Be(13);
        call3.Should().Be(42);
        call4.Should().Be(13);
    }
}
