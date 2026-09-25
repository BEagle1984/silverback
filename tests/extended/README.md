# Extended tests

Open **Silverback.Tests.Extended.sln** on Windows to build the interactive testbench, benchmarks and real-broker integration tests together. Project directories sit directly beside the solution, matching its flat structure. The solution includes all source projects from the main solution under **Src**. **Solution Items** exposes shared configuration, documentation and scripts directly in the IDE.

## Projects

| Project | Purpose |
| --- | --- |
| Silverback.Tests.Extended.TestBench | Interactive WPF testbench |
| Silverback.Tests.Extended.TestBench.Consumer | Containerized testbench consumer |
| Silverback.Tests.Extended.TestBench.Shared | Testbench topic definitions and logging configuration |
| Silverback.Tests.Extended.Integration | xUnit tests against real brokers |
| Silverback.Tests.Extended.Integration.Worker | Containerized application managed by the integration tests |
| Silverback.Tests.Extended.Shared | Messages and subscriber workloads shared by the testbench and integration worker |
| Silverback.Tests.Extended.Benchmarks | General implementation and pipeline benchmarks |
| Silverback.Tests.Extended.Benchmarks.VersionComparison.Current | Benchmarks against the current repository sources |
| Silverback.Tests.Extended.Benchmarks.VersionComparison.V4_6_2 | Benchmarks against Silverback 4.6.2 |

Current-version projects reference the repository sources; no C# sources are linked. The historical version comparison intentionally uses pinned NuGet packages. The general benchmark project's assembly name remains `Silverback.Tests.Performance` for the existing production `InternalsVisibleTo` declarations; its project and namespaces use the new name.

## Build and CI

Run commands from the repository root:

```shell
dotnet build tests/extended/Silverback.Tests.Extended.sln -c Release
```

The WPF application requires Windows. The integration runner and worker also run on Linux. The other-projects Azure pipeline builds the extended solution; its optional `runIntegrationTests` parameter runs the integration project tests on a Linux agent and publishes TRX results and evidence.

The deterministic Kafka ownership tests remain in the main solution and do not require Docker. Extended tests are separate from the default main-suite run.

## Interactive testbench

Start the root Docker Compose infrastructure, then launch **Silverback.Tests.Extended.TestBench** from the extended solution. Its consumer image is built from the current checkout.

Options:

- `--clear-logs`, `-c`: purge testbench log files.
- `--build`, `-b`: rebuild the consumer image.
- `--topics`, `-t`: delete and recreate the testbench topics.

## Integration tests

Docker with Linux containers and the .NET 10 SDK are required. Ports 19092 and 29092 must be available for the root Kafka services; MQTT tests use port 1883.

```shell
dotnet test tests/extended/Silverback.Tests.Extended.Integration/Silverback.Tests.Extended.Integration.csproj --logger trx --results-directory tests/extended/TestResults
```

Traits describe the dependency, broker and purpose independently:

| Trait | Coverage |
| --- | --- |
| `Type=Integration` | All real-broker cases, including stress workloads and diagnostic controls |
| `Type=Unit` | Receipt-verifier tests that run without Docker |
| `Category=Stress` | Continuous consumption under repeated rebalances and finite reconciliation workloads |
| `Category=Diagnostics` | Deliberately blocked polling used to verify stall detection and diagnostic capture |
| `Dependency=Docker` | Tests requiring the root Compose infrastructure |
| `Broker=Kafka` / `Broker=MQTT` | Broker-specific tests; Kafka also includes the local receipt-verifier tests |

Use `--filter "Type=Integration&Category!=Stress&Category!=Diagnostics"` for the functional broker cases, `--filter "Category=Stress"` for load/rebalance workloads, or `--filter "Type=Unit"` for the local verifier. Combine traits, for example `--filter "Type=Integration&Broker=MQTT"`. Omitting the filter runs every case, including the diagnostic control.

Filter `FullyQualifiedName~StaticAssignmentTests` for explicit partition assignment and offset-commit diagnostics or `FullyQualifiedName~RebalanceTests` for finite reconciliation cases. The CI `runIntegrationTests` option runs the complete project, including its local verifier tests.

The xUnit project follows the E2E layout: broker-specific cases in `Kafka/` and `Mqtt/`, reusable container support in `TestHost/`, and infrastructure in `TestHost/Kafka/` and `TestHost/Mqtt/`.

`DockerTestsFixture` starts the required broker services from the root `docker-compose.yaml` using the `silverback` Compose project and `silverback_default` network. It leaves this shared infrastructure running. `ContainerTestRun` uses FluentDocker to manage worker containers. Compose startup and image builds use bounded Docker CLI calls from C#.

Each case uses a unique `stress-` prefix for topics, client/group IDs and containers. Kafka topics are retained for investigation; worker containers are removed after evidence capture. The Kafka fixture builds the worker from the current checkout. `INTEGRATION_SDK_IMAGE` overrides the default `mcr.microsoft.com/dotnet/sdk:10.0` build image.

### Coverage

Reconciliation cases produce a finite manifest and repeatedly join and remove consumers. Durable processing receipts are written to partition-aligned Kafka journals before acknowledging input. The verifier checks all produced records, committed end offsets, strictly increasing offsets within each processing channel, first-processing order across channel replacements, and assignment/revocation boundaries. It also rejects processing from a retired channel and successful commit observations that cross records not yet present in the receipt journal. Commit callbacks are observations, not a continuous audit of the broker: deterministic main-suite tests separately check every store/commit boundary in forced interleavings.

A test-only pipeline behavior stamps each envelope with its assignment epoch and a processing-channel ID derived from sequence-store object identity. Sequence stores are replaced with their channel buffers, so an ID changes at a real processing-lifetime boundary, not merely because an offset repeats. A shared channel can be replaced during cooperative partial revocation without another Kafka assignment callback for retained partitions. Replay across that boundary is allowed; duplicate or backward offsets within the same channel still fail. The receipt journal records each channel's first observed offset and the report counts same-assignment replays.

The matrix covers default eager and cooperative-sticky assignment, independent and shared channels, manual and automatic commits, and partial batches. Cooperative/shared cases exercise both single messages and batches with both commit modes. Assignment/channel identity is captured before batching and remains attached to each envelope during delayed processing.

Static-assignment cases run a Silverback host in the test process against the root Kafka cluster. They compare automatic commits, commits after each message and disabled commits, with a group ID in every case. Each mode runs with an isolated group and with a subscribed native consumer sharing the group ID on a different topic. Assertions check callback errors and actual broker offsets before and after shutdown, plus error logging and continued processing. With the shared group, both automatic and manual commits fail while consumption continues. The cases verify that failures are logged without entering message error handling or replaying the subscriber, with both the default policy and a configured retry policy. These commit rejections are expected reproduction results.

Continuous cases reuse the testbench subscriber and simulated failures across singles, batches and streams, with six partitions, two processing slots and capacity-one buffers. The slow-handler control verifies that the diagnostic detector fires before `max.poll.interval.ms` and captures a stack. A stall candidate alone does not prove a deadlock.

MQTT reconnect cases use the real MQTTnet client and root EMQX cluster through HAProxy. They block a subscriber while another delivery is buffered, disconnect and reconnect, then verify fresh-message progress and ordered replay for persistent QoS 1/2 sessions. QoS 0 and clean sessions explicitly expect unread deliveries to be discarded. The ordinary persistent-session cases use the parameterless API and verify session resumption. A full-buffer case retains an explicit finite expiry, holds a native receive callback waiting for space and verifies bounded disconnect and broker replay. Test sessions are deleted during cleanup. The MQTT application runs in the test process; it does not use the Kafka worker image.

MQTT connection-loss cases cut the client's TCP connection through a test proxy while input buffers are full. They verify cleanup before reconnecting, ordered persistent-session replay at QoS 1/2, and continued consumption. Shared-client variants also publish from the interrupted subscriber and verify that the retry policy and broker replay recover without a circular wait.

MQTT shutdown cases exercise actual hosted shutdown while an in-flight subscriber publishes through the same native MQTT client. An independent client observes the reply. The cases cover QoS 1/2 replies, with and without full input buffers, and hold shutdown open across two observed keep-alive responses before publishing. The main E2E suite also checks single-message and batch subscribers publishing between all Kafka/MQTT source and destination combinations, including separate MQTT clients.

### Worker and evidence

Run the xUnit tests to manage the worker. Its `Kafka/ReconciliationConsumer` records processing and ownership events, `Kafka/MixedWorkload` runs shared subscriber workloads, and `Diagnostics/ProgressProbe` records recent activity and flags polling stalls. The fixture supplies the required `PREFIX` environment variable. The image includes dotnet-stack and dotnet-dump.

Evidence is written under `tests/extended/artifacts/<stress-prefix>/`: settings, logs, container inspection, manifests, receipts, committed offsets and reconciliation reports. Failure cleanup attempts bounded stack and mini-dump capture before stopping live workers. TRX output includes the evidence directory.

Ad hoc investigation summaries are kept in the repository's ignored `temp/` directory. Keep one current summary per investigation and reference its retained evidence under `tests/extended/artifacts/` and `tests/extended/TestResults/`.

## Benchmarks

Run the selected benchmark project in Release configuration, for example:

```shell
dotnet run --project tests/extended/Silverback.Tests.Extended.Benchmarks.VersionComparison.Current -c Release
```

`UpdateOlderVersions.ps1` scaffolds an additional historical comparison from the current-version project. It accepts stable package versions and refuses to overwrite an existing comparison, preserving its API adaptations. Adapt the generated workloads to that version and add the project to the extended solution.
