# Silverback Concurrency Audit — Follow-up Tests

This file tracks audit findings that are not yet covered by a Coyote test in
`tests/Silverback.Tests.Concurrency/`. It exists so reviewers can flip the
failing tests / skipped tests into fixes one at a time without losing the audit
context. Each entry points at the source location and describes what a proper
test would look like.

## Covered in this branch

| # | Test file | Status |
|---|---|---|
| H1 | `SequenceStoreConcurrencyTests.cs` | **Failing** — catches `InvalidOperationException` from the Dictionary enumerator when `AddAsync` races with `GetEnumerator`. |
| C2 | `MessageStreamProviderCoyoteTests.Abort_RacingCompleteAsync_ShouldNotReturnAsSilentNoOp` | **Failing** — catches `InvalidOperationException: "The streams are already completed"` thrown from `Abort()` when it races with `CompleteAsync()`. |
| H4 | `MessageStreamProviderCoyoteTests.CreateStream_RacingPushAsync_...` | **Skipped** — the async enumerator path crashes the Coyote test host with an `AsyncTaskMethodBuilder.SetResult` double-set. Needs a non-enumerator driver. |
| C1 | `SequenceAddCompleteRaceCoyoteTests.CompleteCoreAsync_RacingAbortAsync_...` | **Skipped** — scaffolding and reflection hook into `TestSequence` are in place. The default Coyote runner reports the race as a "potential deadlock" before reaching a concrete bug; the runner needs `WithPotentialDeadlocksReportedAsBugs(false)`, `WithConcurrencyFuzzingEnabled()`, `WithTestingIterations(10_000)`, and the invariant should assert downstream side effects on the `IConsumerTransactionManager` probe rather than terminal enum state. |

## Not covered — tractable with modest harness work

### H2 — `_sequences` iterated while mutated in `CompleteLinkedSequence`

- **Loc:** `src/Silverback.Integration/Messaging/Sequences/SequenceBase`1.cs:181-185, 491-500`.
- **State:** dormant — the only in-tree subclass that sets `trackIdentifiers: false`
  (`UnboundedSequence`) never receives child sequences, so `CompleteLinkedSequence`'s
  `_sequences?.Remove(sequence)` is never hit during a `ForEach` iteration.
- **Test sketch:**
  1. Create a `TestSequence` constructed with `trackIdentifiers: false` (add a ctor
     overload; the production ctor accepts it).
  2. Inject two child sequences into `_sequences` via reflection.
  3. Invoke `((ISequenceImplementation)parent).NotifyProcessingCompleted()` and
     assert no `InvalidOperationException` is thrown from the enumerator.
- This is not inherently a concurrency bug; Coyote is overkill. A plain xunit
  test would catch it just as well, once a `trackIdentifiers: false` subclass
  with child sequences exists in production code.

### H3 — `Dispose()` orders `_streamProvider.Dispose()` before draining pending adds

- **Loc:** `SequenceBase`1.cs:447-480`.
- **Current order:** `_streamProvider.Dispose()` → `_addCancellationTokenSource.Dispose()` →
  `_sequences?.ForEach(s => s.Dispose())` → `_addSemaphoreSlim.Wait()` → `_completeSemaphoreSlim.Wait()`.
- **Bug:** a concurrent `AddAsync` holding `_addSemaphoreSlim` may still be inside
  `_streamProvider.PushAsync` when `_streamProvider.Dispose()` runs, so the push can
  see a disposed provider and throw `ObjectDisposedException`.
- **Test sketch:** requires a controllable subscriber that yields inside `PushAsync`.
  The current drain-pattern crashes the Coyote test host (see H4). An alternative is
  a fake `IMessageStreamProvider` that records a "push in flight" callback — but the
  production `SequenceBase` only accepts `MessageStreamProvider<T>` (concrete class)
  as a ctor param, so the fake would need to inherit from it, and a few of its members
  are `internal sealed`. Tractable but not trivial.
- **Fix direction:** acquire both semaphores first, then dispose.

### C4 — `AddAsync` can observe `IsPending == true` after the stream provider has been completed

- **Loc:** `SequenceBase`1.cs:525-546` (`CompleteCoreAsync`).
- **Bug:** between `await _streamProvider.CompleteAsync(cancellationToken)` returning and
  `_completeState = CompleteState.Complete` being assigned at the end of the method, a
  concurrent `AddAsync` caller can still observe `IsPending == true` (because
  `IsPending => _completeState == CompleteState.NotComplete`) and attempt to push into
  an already-completed stream provider, which throws
  `InvalidOperationException("The streams are already completed or aborted.")` from
  `MessageStreamProvider`1.PushAsync:81`.
- **Test sketch:** same subscriber problem as C1/H3. Can probably be collapsed into
  the C1 harness once that one runs; C4 is essentially the symmetric observation.
- **Fix direction:** move the `_completeState = Complete` assignment *before* the
  `await _streamProvider.CompleteAsync`, or acquire `_completeSemaphoreSlim` around
  the whole tail of `AddCoreAsync` (as in the C1 fix).

## Not covered — requires substantial Kafka fake infrastructure

### C3 — `ConsumerChannelsManager` releases `_messagesLimiterSemaphoreSlim` based on `CurrentCount`, not ownership

- **Loc:** `src/Silverback.Integration.Kafka/Messaging/Broker/Kafka/ConsumerChannelsManager.cs:150-165`.
- **Test sketch:** reproduce the pattern in isolation — a `SemaphoreSlim limiter = new(2, 2)`,
  a helper method that mirrors the conditional-acquire / count-based-release pattern, and
  a Coyote test that concurrently flips `_channels.Count` across the `MaxDegreeOfParallelism`
  boundary. Either writes a mini fake of `ConsumerChannelsManager` (loses regression
  protection) or mocks out enough of `IConsumer`, `IConsumerConfiguration`, `IConsumeLoopHandler`,
  `IKafkaClientWrapper`, `ConsumerChannel<T>` to spin up the real class.
- **Fix direction:** capture `bool acquired` locally and release iff true.

### H5 — Kafka commit TOCTOU on revoked partitions

- **Loc:** `src/Silverback.Integration.Kafka/Messaging/Broker/KafkaConsumer.cs:250-279` (`CommitCoreAsync`) and `152-174` (`OnPartitionsRevoked`).
- **Test sketch:** needs a fake `IConfluentConsumerWrapper` recording `StoreOffset` calls and a
  Coyote-controlled `_revokedPartitions` mutation. Moderate infrastructure investment.

### H6 — `OnPartitionsRevoked` commits after `FireAndForget` consume-loop stop

- **Loc:** `KafkaConsumer.cs:152-174`.
- **Test sketch:** same fake as H5. Model `StopAsync` as a task that only completes when the
  test releases a gate; assert `Client.Commit()` is not called before that.

### H7 — `ConsumeLoopHandler.Start/Stop` racy on non-volatile `IsConsuming`

- **Loc:** `src/Silverback.Integration.Kafka/Messaging/Broker/Kafka/ConsumeLoopHandler.cs:54-102`.
- **Test sketch:** race two `Start()` callers and assert only one consume-loop task runs. Requires
  mocking `IConfluentConsumerWrapper` and friends. Lower-value than C1/C2/C3.

## Not covered — low value under Coyote

### M1 — `ConsumerChannel.Reset` non-atomic channel reassignment

- **Loc:** `src/Silverback.Integration/Messaging/Broker/ConsumerChannel`1.cs:76-84`.
- Works today because callers await stop-then-reset. Document the precondition with an XML
  comment on `Reset`; regression-test via a plain unit test if someone adds a caller that
  violates the precondition.

### M2 — `OffsetsTracker.TrackOffset` two non-atomic `AddOrUpdate` calls

- **Loc:** `src/Silverback.Integration.Kafka/Messaging/Broker/Kafka/OffsetsTracker.cs:27-42, 70-79`.
- A test would compose a reader that sees `rollback > commit + 1`. Straightforward with Coyote
  once you have the concrete dictionaries; deferred because the observable bug is subtle and
  only matters at rebalance time.

### M3 — `SequenceBase._completeState` non-volatile int

- **Loc:** `SequenceBase`1.cs:47, 556-573`.
- Timer loop reads the non-volatile field. On ARM64 the timer may lag observing the new
  state for longer than intended. The fix is `Volatile.Read` or marking the field volatile.
  Under Coyote on x64 this will probably never reproduce because x64 has a stronger memory
  model.

### L1 — `AbortCoreAsync` unconditional `_completeState = Aborted` write

- Subsumed by C1; will be fixed as part of the C1 fix.

### L2 — `AsyncEvent<TArg>.InvokeAsync` runs handlers synchronously under a `System.Threading.Lock`

- **Loc:** `src/Silverback.Core/AsyncEvent`1.cs:68-79`.
- Not a correctness bug — `System.Threading.Lock` is reentrant on net9+. Noted in the audit
  for completeness; no test warranted.

## Harness notes

Three structural issues surfaced while writing this batch. Each affects more than one test:

1. **`MessageStreamEnumerable<T>` async enumerator vs Coyote rewriter.** The
   `await foreach` pattern against a rewritten `IMessageStreamEnumerable<T>` crashes the
   Coyote test host with `AsyncTaskMethodBuilder.SetResult` being called twice. Needs an
   alternative subscriber driver (e.g. a direct `IAsyncEnumerator<T>` loop with manual
   `MoveNextAsync`, or an in-memory fake of `ILazyMessageStreamEnumerable` that bypasses
   the state machine entirely). Affects H3, H4, C1, C4.
2. **Coyote's "potential deadlock" heuristic triggers on legitimate lock inversions that
   eventually terminate.** The C1 race is a real inversion, but the terminating schedule
   (T1's `provider.CompleteAsync` throws "already aborted" once T2's `AbortIfPending`
   ran first, T1 releases the add semaphore via its finally, T2 proceeds) is dismissed
   as a suspected hang. Use `Configuration.WithPotentialDeadlocksReportedAsBugs(false)`
   plus `WithDeadlockTimeout(...)` and `WithConcurrencyFuzzingEnabled()`.
3. **NSubstitute mocks show up as "uncontrolled invocations".** Coyote's scheduler cannot
   instrument them, but this is cosmetic: the tests still find bugs. If we want cleaner
   output, replace NSubstitute with hand-rolled fakes in the Coyote-sensitive test paths.
