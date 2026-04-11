// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Concurrency;

// Audit finding H1 — SequenceStore is a plain Dictionary<string, ISequence> with zero
// synchronization. Cross-thread callers exist today: the main channel thread calls
// AddAsync / GetAsync during pipeline flow, while the SequenceBase timeout timer task,
// external abort paths, and disposal all call RemoveAsync or enumerate the store from
// a different thread.
//
// The test reproduces the race by concurrently adding a new entry while enumerating the
// store. The underlying Dictionary's enumerator has a version check that throws
// InvalidOperationException("Collection was modified...") if it observes a mutation
// across MoveNext calls. Coyote's scheduler can reliably interleave the mutation between
// the two MoveNext calls that bracket an `await Task.Yield()` in the enumerator task.
//
// Expected: this test fails on master (InvalidOperationException bubbles out of
// Task.WhenAll). Fix direction: wrap _store in a lock, or switch the backing field to
// ConcurrentDictionary. See audit H1.
public class SequenceStoreConcurrencyTests
{
    private readonly ITestOutputHelper _output;

    public SequenceStoreConcurrencyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SequenceStore_EnumerateWhileAdding_ShouldNotThrow()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());

                // Seed with one entry so the enumerator actually iterates and hits a
                // subsequent MoveNext after the race-inducing yield.
                ISequence seed = Substitute.For<ISequence>();
                seed.SequenceId.Returns("seed");
                await store.AddAsync(seed).ConfigureAwait(false);

                ISequence newSequence = Substitute.For<ISequence>();
                newSequence.SequenceId.Returns("added-concurrently");

                // T1: add a new entry. AddAsync is async but synchronous in this path
                // (the await-AbortAsync branch is only taken when the key already exists).
                Task add = Task.Run(
                    async () => await store.AddAsync(newSequence).ConfigureAwait(false));

                // T2: enumerate the store. An `await Task.Yield()` between MoveNext calls
                // gives Coyote a scheduling point at which to interleave the mutation.
                Task enumerate = Task.Run(
                    async () =>
                    {
                        using IEnumerator<ISequence> enumerator = store.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            await Task.Yield();
                        }
                    });

                await Task.WhenAll(add, enumerate).ConfigureAwait(false);
            },
            _output);
    }
}
