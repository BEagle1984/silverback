# Extended tests

Open **Silverback.Tests.Extended.slnx** on Windows to build the interactive testbench, benchmarks and real-broker integration tests together. Project directories sit directly beside the solution, matching its flat structure. The solution includes all source projects from the main solution under **Src**. **Solution Items** exposes shared configuration, documentation and scripts directly in the IDE.

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
dotnet build tests/extended/Silverback.Tests.Extended.slnx -c Release
```

The WPF application requires Windows. The integration runner and worker also run on Linux. The `other-projects` GitHub workflow builds the extended solution on the pushes to main and on the pull requests, to verify that it compiles. The `extended-integration-tests` workflow runs the integration project tests on a Linux runner every night and on demand, with an optional test filter, and publishes the TRX results and the evidence. It isn't a required check.

The deterministic Kafka ownership tests remain in the main solution and do not require Docker. Extended tests are separate from the default main-suite run.

## Interactive testbench

Start the root Docker Compose infrastructure, then launch **Silverback.Tests.Extended.TestBench** from the extended solution. Its consumer image is built from the current checkout.

Options:

- `--clear-logs`, `-c`: purge testbench log files.
- `--build`, `-b`: rebuild the consumer image.
- `--topics`, `-t`: delete and recreate the testbench topics.

## Integration tests

Docker with Linux containers and the .NET 10 SDK are required. The Kafka tests start the root Kafka services (Kafka 4.x), which publish ports 19092 and 29092. The schema registry tests also start the Confluent schema registry, which publishes port 8081. The MQTT tests start EMQX and HAProxy; HAProxy also starts the Kafka services and publishes ports 1883, 8083, 18083 and 9092. These ports must be free, unless the containers of the `silverback` Compose project already hold them.

The Compose images are pinned, so that local and CI runs use the same broker versions. The fixtures reuse the existing Compose containers as they are (`--no-recreate`), so after `docker-compose.yaml` changes (images or service settings), recreate the containers once by running `docker compose up -d kafka-1 kafka-2 schema-registry emqx1 emqx2 haproxy` from the repository root. Kafka containers created from a 3.x image fail the consumer-protocol cases.

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

Filter `FullyQualifiedName~StaticAssignmentTests` for explicit partition assignment and offset-commit diagnostics or `FullyQualifiedName~RebalanceTests` for finite reconciliation cases. The scheduled CI run executes the complete project, including its local verifier tests.

The xUnit project follows the E2E layout: broker-specific cases in `Kafka/` and `Mqtt/`, reusable container support in `TestHost/`, and infrastructure in `TestHost/Kafka/` and `TestHost/Mqtt/`.

`DockerTestsFixture` starts the required broker services from the root `docker-compose.yaml` using the `silverback` Compose project and `silverback_default` network. It leaves this shared infrastructure running. `ContainerTestRun` uses FluentDocker to manage worker containers. Compose startup and image builds use bounded Docker CLI calls from C#.

The tests that run Silverback in the test process derive from `BrokerHarnessTests<THarness>` and build the host through its `Harness`, an `MqttTestHarness` or a `KafkaTestHarness` (both based on `BrokerTestHarness`) in `TestHost/`. xUnit creates a harness for each test case and disposes it after the test, so that a teardown failure, for example a client that doesn't stop, fails the test and is reported together with the failure of the test rather than in its place. A harness creates the unique prefix, builds the host with a log collector, offers the clients and helpers of its broker, and on dispose stops everything, deletes the MQTT sessions and writes the collected logs and the teardown failures to the test output. It also hides the waits that the clustered brokers need before a test can rely on them:

- The EMQX nodes replicate the subscriptions asynchronously, and HAProxy spreads the connections across the nodes, so a message published right after a subscription is acknowledged can be dropped when the publisher is connected to the other node. The `MqttTestHarness` methods that connect the Silverback clients (`ConnectAsync`, `StartHostAsync`, `ReconnectAsync`, `WaitUntilSubscribedAsync`) and `TestMqttClient.SubscribeAsync` return only once every node routes the subscribed topics. Publish only after they return, and don't wait on the `Subscribed` event or subscribe through the native MQTTnet client.
- `CreateTopics` can return before both Kafka brokers know the new topic, and a consumer joining a group in the meantime can receive an empty assignment that is never repaired. `KafkaTestHarness.CreateTopicsAsync` returns only once every broker knows the topics.

The scenario itself (the gates, a disconnect, a shutdown, the assertions) stays in the test, which keeps access to the host, the clients and the native MQTTnet client for what the harness doesn't cover.

Each case uses a unique `stress-` prefix for topics, client/group IDs and containers. Kafka topics are retained for investigation; worker containers are removed after evidence capture. The Kafka fixture builds the worker from the current checkout. `INTEGRATION_SDK_IMAGE` overrides the default `mcr.microsoft.com/dotnet/sdk:10.0` build image.

### Coverage

Reconciliation cases produce a finite manifest and repeatedly join and remove consumers. Durable processing receipts are written to partition-aligned Kafka journals before acknowledging input. The verifier checks all produced records, committed end offsets, strictly increasing offsets within each processing channel, first-processing order across channel replacements, and assignment/revocation boundaries. It also rejects processing from a retired channel and successful commit observations that cross records not yet present in the receipt journal. Commit callbacks are observations, not a continuous audit of the broker: deterministic main-suite tests separately check every store/commit boundary in forced interleavings.

A test-only pipeline behavior stamps each envelope with its assignment epoch and a processing-channel ID derived from sequence-store object identity. Sequence stores are replaced with their channel buffers, so an ID changes at a real processing-lifetime boundary, not merely because an offset repeats. A shared channel can be replaced during cooperative partial revocation without another Kafka assignment callback for retained partitions. Replay across that boundary is allowed; duplicate or backward offsets within the same channel still fail. The receipt journal records each channel's first observed offset and the report counts same-assignment replays.

The matrix covers the classic protocol with default eager and cooperative-sticky assignment and the KIP-848 consumer protocol (`GROUP_PROTOCOL=Consumer`, where the broker assigns the partitions incrementally), independent and shared channels, manual and automatic commits, and partial batches. Cooperative/shared cases exercise both single messages and batches with both commit modes. Assignment/channel identity is captured before batching and remains attached to each envelope during delayed processing.

Static-assignment cases run a Silverback host in the test process against the root Kafka cluster. They compare automatic commits, commits after each message and disabled commits, with a group ID in every case. Each mode runs with an isolated group and with a subscribed native consumer sharing the group ID on a different topic. Assertions check callback errors and actual broker offsets before and after shutdown, plus error logging and continued processing. With the shared group, both automatic and manual commits fail while consumption continues. The cases verify that failures are logged without entering message error handling or replaying the subscriber, with both the default policy and a configured retry policy. These commit rejections are expected reproduction results.

Topic-pattern cases also run a Silverback host in the test process and subscribe a `^` regular expression, once with each group protocol: with the classic protocol librdkafka matches the topics on the client, while with the KIP-848 consumer protocol the broker matches them and requires the pattern to match the entire topic name. They verify that only the matching topics are assigned and consumed, that a matching topic created while consuming is picked up at the next client metadata refresh (classic protocol) or when the broker resolves the pattern again (consumer protocol), and that an endpoint subscribing the topic name takes precedence over a matching pattern. A pattern without the trailing `$` shows the different match semantics of the two protocols. Filter `FullyQualifiedName~TopicPatternSubscriptionTests` to run them.

Schema registry cases run a Silverback host in the test process against the root Kafka cluster and Confluent schema registry. Their fixture doesn't build the worker image. They produce and consume Avro, JSON and Protobuf messages with automatic schema registration and check the subject registered for the topic value and the schema ID in each message. With automatic registration disabled, they verify that the producer uses a schema registered in advance, and that producing fails while no schema is registered. Two Avro cases check the interoperability with the Confluent serializers: Silverback consumes the messages produced by the Confluent serializer, and a Confluent generic-record deserializer reads the messages produced by Silverback. The registered subjects are retained along with the topics. Filter `FullyQualifiedName~SchemaRegistryTests` to run them.

Continuous cases reuse the testbench subscriber and simulated failures across singles, batches and streams, with six partitions, two processing slots and capacity-one buffers. The mixed workload also runs with the KIP-848 consumer protocol (`GROUP_PROTOCOL=Consumer`), and each case waits until every joining member was assigned partitions. The slow-handler control verifies that the diagnostic detector fires before `max.poll.interval.ms` and captures a stack. A stall candidate alone does not prove a deadlock.

MQTT reconnect cases use the real MQTTnet client and root EMQX cluster through HAProxy. They block a subscriber while another delivery is buffered, disconnect and reconnect, then verify fresh-message progress and ordered replay for persistent QoS 1/2 sessions. QoS 0 and clean sessions explicitly expect unread deliveries to be discarded. The ordinary persistent-session cases use the parameterless API and verify session resumption. A full-buffer case retains an explicit finite expiry, holds a native receive callback waiting for space and verifies bounded disconnect and broker replay. Test sessions are deleted during cleanup. The MQTT application runs in the test process; it does not use the Kafka worker image.

MQTT connection-loss cases cut the client's TCP connection through a test proxy while input buffers are full. They verify cleanup before reconnecting, ordered persistent-session replay at QoS 1/2, and continued consumption. Shared-client variants also publish from the interrupted subscriber and verify that the retry policy and broker replay recover without a circular wait.

MQTT shutdown cases exercise actual hosted shutdown while an in-flight subscriber publishes through the same native MQTT client. An independent client observes the reply. The cases cover QoS 1/2 replies, with and without full input buffers, and hold shutdown open across two observed keep-alive responses before publishing. The main E2E suite also checks single-message and batch subscribers publishing between all Kafka/MQTT source and destination combinations, including separate MQTT clients.

MQTT topic-filter cases run a Silverback host in the test process against the root EMQX cluster. They verify that a multi-level wildcard also matches its parent level, that a single-level wildcard matches exactly one level, including an empty one, and that two clients sharing a subscription consume each message once. Each message must be routed to the endpoint of its actual topic, without the `$share` prefix. Probe messages are published until one is consumed, since the cluster nodes replicate the subscriptions asynchronously. Filter `FullyQualifiedName~MqttTopicFilterTests` to run them.

MQTT batch-processing and chunking cases run a Silverback host in the test process against the root EMQX cluster, with a persistent session, and record the deliveries and acknowledgments on the wire. The batch cases cover QoS 1/2 batches, concurrent batches acknowledged out of order with parallel processing, and a pending batch replayed in order by the broker after a disconnect or a connection loss. They also cover a receive maximum equal to the batch size multiplied by the degree of parallelism, and a batch larger than the EMQX in-flight limit (`max_inflight`), which completes by its max wait time: the configuration rejects a client receive maximum below the batch capacity. The chunking cases produce chunked messages while the consumer is disconnected and verify their reassembly with and without parallel and batch processing, and the chunk user properties observed by an independent client. At the end, each case resumes the session with a native client and verifies that the broker replays no unacknowledged delivery. Before publishing, the cases wait until both nodes route the topic, through the EMQX command line, since the probe messages would be added to the batches. Filter `FullyQualifiedName~Mqtt.BatchProcessingTests|FullyQualifiedName~Mqtt.ChunkingTests` to run them.

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
