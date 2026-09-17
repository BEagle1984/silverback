> Historical investigation from before the extended-test migration. Run IDs, commands, paths and versions below describe those original experiments. Use ../README.md for the current xUnit runner.

# Kafka overflow wake-up investigation — 2026-09-16

## Reproduction

The starting checkout was `f180e99b18cf9582793589b173e00d2370e231ff`. Tests used the checkout's Silverback 5.5.3 code, Confluent.Kafka/librdkafka 2.15.0, .NET 10.0.8 in Linux Docker containers, and Kafka 4.2.0. The partition assignment strategy was left unset. Processing used the existing testbench subscriber methods.

The original code stalled in both a mixed workload and a single-message workload shortly after the first consumer join. Ordinary simulated processing delays were 0–7 ms. The deliberately delayed control was a separate experiment.

With automatic recovery enabled, the single-message run `sb-stress-20260916-184908` stopped progressing around 16:49:23 UTC. It had no recorded application errors, and the ThreadPool queue was empty while stalled. At 16:54:23.443 UTC it logged:

> Application maximum poll interval (300000ms) exceeded by 62ms ... leaving group

Silverback immediately logged that it would try to recover. By 16:54:39 UTC the consumed count had advanced from 4,479 to 13,854 and the processed count from 553 to 1,712. Consumer statistics were arriving again. This reproduces the warning and recovery pattern; it does not retrospectively prove the cause of every production incident.

## Defect

`ConsumerChannel<T>` has a bounded main queue and an overflow queue. When a Kafka write is canceled during a rebalance, the consumed record is redirected to overflow.

A problematic interleaving is:

1. The reader checks the overflow queue, finds it empty, and awaits a read from the empty main queue.
2. A canceled Kafka write places a message in overflow.
3. The next normal write waits for overflow to drain, preserving the existing ordering rule.
4. The reader cannot see the overflow message because it is waiting exclusively for the main queue. The writer cannot reach the main queue until that reader drains overflow.

The source of the lost wake-up is the two-queue handoff, not the subscriber workload. No monitor lock cycle is required. Kafka's native background thread eventually reports MAXPOLL. Cancellation during Silverback recovery can release the stuck write.

The mixed-run dump `sb-stress-20260916-184403` provides matching object evidence:
- Poll thread: `ConsumeLoopHandler.ConsumeOnce → ConsumerChannelsManager.Write → SafeWait`.
- Writer async object `00007f86d4ae1550`: `WriteAsync` state 0, awaiting the task-based delay in the overflow-drain loop.
- Writer's channel: `00007f86d49f1f90`.
- Reader async object `00007f86d4a027c8`: `ReadAsync` state 0, awaiting the main channel read, with the **same** channel object `00007f86d49f1f90`.
- Other channel readers were idle; ThreadPool work was not queued.

Raw dumps, stacks, and traces remain in the ignored `artifacts/` directory. The original image is retained locally as `silverback-testbench-stress:before-fix`.

## Correction and validation

The deterministic regression starts an empty read, adds an overflow message, and requires that read to complete. It timed out on the original code without Kafka or a random scheduler race.

The correction checks overflow first, then the main queue, and waits for availability on either queue when both are empty. It cancels the unused availability wait so waiters do not accumulate. It does not race two consuming reads, which could otherwise remove an extra message.

Added checks cover the lost wake-up, subsequent normal writes, normal queue wake-up, cancellation, completion, and reset. Existing ordering/backpressure tests are retained.

Validation results:
- New regression: failed on original code with `TimeoutException`; passed after the correction.
- Channel tests: 12 passed.
- Integration unit tests: 1,157 passed, 2 existing skips.
- Relevant Kafka/MQTT end-to-end tests: 78 passed.
- Fixed single-message Docker run `sb-stress-20260916-185458`: 180 seconds of repeated consumer joins/leaves, no stall candidate or application errors; final primary counters approximately 34,719 consumed and 16,377 processed.
- Fixed mixed-workload Docker run `sb-stress-20260916-185821`: 184 seconds of repeated consumer joins/leaves, no stall candidate or application errors; final primary counters 28,062 consumed and 20,727 processed. Final statistics age was 0.715 seconds. Automatic recovery was enabled in both fixed stress runs, and the assignment strategy remained unset.

The availability race adds temporary wait/cancellation objects only when both queues are empty. Throughput benchmarking was not part of this investigation. Longer runs and production-version/configuration matching remain useful, especially for other independent failure modes.

## Shutdown checkpoint

Paused at the user's request on 2026-09-16. All pending tests finished, including the fixed mixed-workload Docker run. Its result is `completed: true, stallCandidate: false`. The stress runner removed its containers and network; the final `docker ps` was empty. No further tests were started.

The source fix, regression tests, headless stress runner, documentation, and changelog changes are saved but uncommitted. Evidence remains under this directory's ignored `artifacts/` folder. The original diagnostic Docker image is retained as `silverback-testbench-stress:before-fix`; the tested corrected image is `silverback-testbench-stress:local`.

There are no pending test processes to resume. When work resumes, first review the saved fix and evidence. Optional follow-up work is a longer stress run, allocation/throughput measurement for empty-queue waits, and comparison against the exact production versions and settings. Reproducing this defect does not establish that it caused every production incident.
## Polling lifecycle simplification — 2026-09-17

The original correction, regression tests, and stress runner are committed as `43f57de3ddca26fafe0b6de21a43243720ba1852` on `codex/kafka-poll-deadlock-backup`. The exploratory checkout is on the separate branch `codex/kafka-rebalance-polling`, based on that commit.

### Preserving the revoked-buffer discard requirement

Keeping the polling loop alive does not require keeping revoked channels alive. Native rebalance callbacks execute synchronously inside `Consume`; that polling thread cannot concurrently write a record to Silverback's channels while it is executing the callback. The callback can still cancel and await the revoked channel readers, complete and remove their channels, handle offsets, and return. Reassignment then creates fresh readers/channels. Records already processed but not committed may still be replayed under at-least-once delivery; discarding old buffers avoids processing stale queued copies in addition to Kafka's redelivery.

The prototype changes only `KafkaConsumer.OnPartitionsRevoked`: it retains the revoked-channel stop/removal and offset handling, removes polling-loop cancellation, and removes the background polling-loop restart method. Actual consumer stop/disconnect still stops the polling loop. The corrected overflow queue is retained until those remaining cancellation paths have been assessed separately.

A deterministic test blocks processing of record 0, enqueues records 1 and 2, and stops the channel reader without canceling its polling token. It verifies that 1 and 2 are discarded, then starts a replacement reader and explicitly redelivers 1 and 2 followed by 3. The observed sequence is exactly 0, 1, 2, 3. Both per-partition and shared-channel modes pass. This validates the discard mechanism independently of Kafka timing; it is not an exactly-once delivery test.

### Validation and limits

- Focused buffer-discard tests: 2 passed, included in the Kafka unit suite.
- Kafka unit suite: 438 passed.
- Existing Kafka rebalance, retry, batch, and consumer tests: 78 passed. These use the repository's broker mocks; native Kafka is checked separately below.
- Build with analyzers: zero warnings and zero errors.
- Native Docker mixed-workload run with default assignment strategy: `sb-stress-20260917-095332` completed 180 seconds of consumer churn with no stall candidate. Final primary counters were 25883 consumed, 20779 processed, and 0 unexpected application errors. Maximum sampled statistics age was 0.942 seconds. Automatic recovery was enabled. All containers created by the run were removed.

The supported direction is to decouple polling-loop lifetime from assignment lifetime, retaining the current revoked-reader shutdown. The current evidence does not justify deleting overflow handling for every shutdown/reconnect path or promising the absence of all independent rebalance races. Cooperative/shared-channel combinations, shutdown concurrent with rebalance, and long-running production-like runs would need further validation before treating this as a release-ready lifecycle change.

The user chose to keep the backup local and not push it to the remote yet. Commit 43f57de3d remains on the local backup branch. Raw diagnostic artifacts are ignored by Git and remain local. The prototype changes are uncommitted on the separate exploratory branch. The 78-test Kafka integration run included the two cooperative-sticky rebalance tests (normal consumption and pending-batch abort/recovery), using broker mocks. The native Docker prototype run used the default assignment strategy; cooperative-sticky has not yet been validated against the real broker for this prototype.

## Rebalance correctness regressions — 2026-09-17

Added `KafkaRebalanceLifecycleTests` with 13 deterministic cases. These exercise the real `KafkaConsumer`, polling loop, channels, commit code, and rollback code, with only the native client scripted. Native callbacks execute synchronously inside the scripted `Consume` call. Explicit gates control the interleavings instead of relying on scheduler timing.

Coverage:
- Revocation and reassignment inside the same poll call, including the first record returned by that call. Old buffered records are discarded; replay proceeds in partition order. Covers eager shared/independent channels and cooperative independent channels.
- Partial cooperative revocation while a retained partition has buffered work. Its original channel continues in order.
- Shutdown overlapping revocation and attempted reassignment. Interrupted and buffered records remain uncommitted and replay after restart.
- Rollback requested after revocation begins; it must not seek or resume that partition.
- A pending rollback overtaken by revocation, with and without subsequent reassignment, for shared and independent channels.
- Ordinary rollback still resumes the current assignment and replays its discarded buffer in order.

The harness records processing starts/completions and checks each committed offset against completed records, rejecting a commit that skips unfinished work. Epoch-tagged records distinguish old buffered copies from expected Kafka redelivery. These are controlled lifecycle tests, not an exhaustive proof of all possible interleavings or of arbitrary subscriber side effects.

### Additional race found by the tests

The delayed rollback continuation previously resumed revoked partitions. If reassignment had already happened, it could also reset the replacement channel. Both controlled schedules failed before the guard was added. `KafkaConsumer` now records assignment versions and validates them before the continuation resets, starts, or resumes anything. A short lock serializes that validation/mutation with assignment changes; it is never held while awaiting channel shutdown. Shared-channel reset is skipped when its captured assignment is obsolete.

This is an additional change to the polling-lifecycle prototype. The original overflow fix remains preserved on the unchanged local backup branch.

Validation: 451 Kafka unit tests passed, including the 13 lifecycle cases and the two earlier channel-discard cases; 100 existing Kafka rebalance, error-policy, batch, and consumer-endpoint integration tests passed. The Kafka unit build with analyzers has no warnings.

### Finite native-broker reconciliation

`run-reconciliation.ps1` runs a finite workload in isolated Docker containers. The producer writes an acknowledged manifest. Each subscriber writes durable processing receipts and assignment/revocation boundaries to a separate Kafka topic partition before returning successfully. After consumer churn, the runner waits for source commits, stops every consumer gracefully, and verifies the complete receipt journal against the manifest and final committed offsets.

Checks cover missing or mismatched records, non-increasing offsets within an assignment, out-of-order first processing of distinct records, processing outside a recorded assignment, and final commits reaching every produced record. Replays across assignments are counted and allowed. Each receipt partition provides ordering without depending on clocks across containers. This workload uses ordinary single-message processing, independent partition channels, manual commits, and no injected processing failures; unit and existing integration tests cover the controlled rollback/batch cases separately.

```powershell
./testbench/Silverback.Tests.Extended.TestBench.Stress/run-reconciliation.ps1
./testbench/Silverback.Tests.Extended.TestBench.Stress/run-reconciliation.ps1 -Assignor CooperativeSticky -SkipBuild
```

The default workload is six partitions with 250 records each and 40 ms subscriber delay, with three consumer joins and departures. The initial 6,000-record attempt exceeded the harness's original drain timeout while processing was still advancing; it is not counted as a correctness pass. The runner now budgets drain time from the record count.

Default-strategy run `sb-audit-20260917-105311`: 1,500 unique records processed, six replay receipts, zero missing records or ordering/assignment violations; all six committed offsets reached 250. There were 42 per-partition assignment and 42 revocation events. Its final cleanup encountered a runner typo after verification succeeded; the typo was fixed, logs were saved, and all of that run's containers/network were removed separately. Evidence includes that distinction in `result.json`.
Cooperative-sticky run `sb-audit-20260917-105549`: 1,500 unique records processed, one replay receipt, zero missing records or ordering/assignment violations; all six committed offsets reached 250. There were 24 per-partition assignment and 24 revocation events. The runner completed successfully and removed its containers/network.

Both successful native runs used image `silverback-testbench-stress:reconciliation`, SHA-256 `316b2022892a6a841ce6ef619363954b73624d3492245d50c363318543042a87`. Reports, acknowledged manifests, container logs, schedules, and image/configuration metadata are saved under the corresponding ignored `artifacts/` directories. The finite runs support the tested normal-rebalance behavior; they do not promise exactly-once delivery or cover process crashes, partition loss, subscriber effects outside the awaited handler, or every timing/configuration combination.