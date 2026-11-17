// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Silverback.Tests.Core.Testing;

public class TestingCollectionTests
{
    private const int ItemsCount = 200_000;

    [Fact]
    public async Task Enumeration_ShouldNotThrowOrTear_WhenItemsAreAddedConcurrently()
    {
        TestingCollection<int> collection = [];

        Task writer = Task.Run(
            () =>
            {
                for (int i = 0; i < ItemsCount; i++)
                    collection.Add(i + 1);
            });

        Exception? exception = null;
        int enumerations = 0;

        // At least one enumeration, also if the writer completes before the loop is entered
        do
        {
            try
            {
                int[] snapshot = [.. collection];

                for (int i = 0; i < snapshot.Length; i++)
                {
                    if (snapshot[i] != i + 1)
                        throw new InvalidOperationException($"Torn result at index {i}.");
                }

                enumerations++;
            }
            catch (Exception ex)
            {
                exception = ex;
                break;
            }
        }
        while (!writer.IsCompleted);

        await writer;

        exception.ShouldBeNull();
        enumerations.ShouldBeGreaterThan(0);
        collection.Count.ShouldBe(ItemsCount);
    }

    [Fact]
    public async Task IndexerAndCount_ShouldBeConsistent_WhenItemsAreAddedConcurrently()
    {
        TestingCollection<int> collection = [];

        Task writer = Task.Run(
            () =>
            {
                for (int i = 0; i < ItemsCount; i++)
                    collection.Add(i + 1);
            });

        while (!writer.IsCompleted)
        {
            int count = collection.Count;
            if (count > 0)
                collection[count - 1].ShouldBe(count);
        }

        await writer;

        collection.Count.ShouldBe(ItemsCount);
    }
}
