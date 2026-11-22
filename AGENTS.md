# Repository guidance

## Temporary investigation files

- Use the repository's top-level `temp/` directory for human-readable Markdown investigation reports. Keep one current summary per investigation, updating the findings, evidence and status instead of accumulating overlapping reports. Distinguish committed fixes, pending fixes and open issues.
- Give reports meaningful names including the creation date: `YYYY-MM-DD-description.md`, for example `2026-09-18-kafka-rebalance-findings.md`. Keep the filename stable when updating an existing summary; update the date inside it.
- Do not retain disposable editing scripts, scratch projects, build output or duplicate logs in `temp/`. Prefer inline commands or the operating system's temporary directory for one-off helpers; delete any repository-local scratch files before finishing the task.
- Put test logs, TRX files and other generated evidence in the existing test/tool output directories. Retain only evidence needed for unresolved findings or meaningful verification, reference it from the report, and remove redundant outputs. Do not discard an unexplained failure merely because a retry passes.
- `temp/` is ignored by Git. Do not commit or force-add reports, add them as Solution Items, or create permanent documentation links to individual temporary files. Keep maintained documentation in its normal location.

## Code conventions

- Follow the applicable `.editorconfig` and the surrounding code, including the existing main-solution style when adding extended tests. Formatting tools are a first pass; review logical grouping manually afterward.
- Use blank lines to separate logical steps. Normally leave a blank line before an `if`, loop, `lock`, `try`, or final `return` when it follows other statements, and after a guard clause before continuing the method. Do not insert blank lines immediately inside braces or between a condition and its body.
- Keep closely related declarations and operations together, but separate setup, execution, synchronization and assertions in tests. Separate independent mock/callback registrations. Do not compress unrelated statements into one uninterrupted block or add a blank line after every statement mechanically.
- Expand dense configuration initializers, long argument lists and fluent configuration chains over multiple lines. Use one property or configuration step per line when a block contains several settings; short, obvious initializers can remain inline.
- Use braces for loops, following the repository's Rider settings. Keep simple single-statement guards compact where the surrounding code does so.
- Keep formatting and readability changes behavior-preserving; do not combine them with unrelated refactoring or bug fixes.
- Do not add a trailing period to simple, single-sentence code comments.
- Give every variable and parameter a meaningful name, including the lambda parameters: a lambda is no different from any other method. Write `properties.OrderBy(property => property.Name)`, not `properties.OrderBy(p => p.Name)`. Single letters are acceptable only where they are the conventional name, as they would be in a `for` loop or a formula: `i` for an index, `n` for a number or count, `x` and `y` for the two sides of a comparison, and the `_` discard for an unused parameter. Any other short name needs a specific reason.
- Use primary constructors (positional parameters) for records, but never for classes or structs: declare a regular constructor, assigning the dependencies to `private readonly` fields.
- Name internal implementation methods with the `Core` suffix, before `Async` when applicable: `Stop` -> `StopCore`, `StopAsync` -> `StopCoreAsync`.
- Synchronize shared state with `lock`, `SemaphoreSlim`, `Interlocked` or tasks. Do not use `volatile` or `Volatile.Read`/`Volatile.Write` "just in case" or for weakly ordered CPUs: use them only where the code relies on the order of reads or writes of different fields without a lock, or on a loop without calls seeing a new value, and name that order in a comment.
- Document with `<exception cref="...">` the exceptions that a caller of the public and protected API can expect and act on: the configuration errors of `Build()` and `Validate()`, the produce and routing errors, the invalid-state and unsupported-operation errors, the timeouts of the testing helpers, and Silverback's own exceptions that surface from a public member (e.g. `UnhandledMessageException` or `OutboxProcessingException`). Write the tag text as the condition, without "Thrown if": `<exception cref="TimeoutException">The timeout elapsed before the outbox was emptied.</exception>`. Put the tag on the interface or abstract member, which the implementations inherit through `<inheritdoc />`, and add tags to a concrete type only for what it throws in addition.
- Do not document `ArgumentNullException`, which the nullable annotations already express, `ObjectDisposedException`, the `OperationCanceledException` of a passed `CancellationToken` (except for the testing helpers that wait), the exceptions that only propagate from user code, Confluent.Kafka or MQTTnet, or the failures of internal invariants. State the allowed values in the `<param>` text instead of an `<exception>` tag (e.g. "Must be greater than zero.").
- Before considering a change done, check the changed C# files for the Rider inspection warnings, which the build does not report: `dotnet jb inspectcode Silverback.slnx --no-build --include="<file>;<file>" --severity=WARNING --format=Text --output=<report>`, with the report in the operating system's temporary directory (it takes about 1.5 minutes). Fix the warnings, or suppress false positives with `[SuppressMessage("ReSharper", "<InspectionId>", Justification = "...")]`, as the surrounding code does.

## Test organization

- Unit tests have one test class per production type, named `<ProductionType>Tests`, in the corresponding unit test project. Mirror the production type's relative folder and namespace beneath the test project's root namespace. Omit generic arity from the test class name, following the existing convention.
- Add coverage to the existing test class instead of introducing a separate feature- or scenario-named class. Only split unusually large classes into partial files when needed; retain the same class and namespace, using filenames such as `<ProductionType>Tests.Feature.cs`.
- E2E and extended integration tests may be organized by feature or use case. This exception does not apply to unit test projects, including unit tests that exercise several collaborating components.

## Running tests

- "Run the tests" means the main solution: `dotnet test --solution Silverback.slnx`. The tests tagged `Dependency=Docker` (PostgreSQL) start their own containers, so they need only a running Docker (CI runs them too).
- Do not run the extended integration tests (`tests/extended`) unless the user asks for them. Offer them when a change affects broker client behavior, such as consumer or producer logic, rebalancing, or a Confluent.Kafka or MQTTnet upgrade. A run takes over 12 minutes and builds Docker images from the working tree, so do not edit files while it runs.
- The extended tests build with the locally cached `mcr.microsoft.com/dotnet/sdk:10.0` image. If the build fails with "A compatible .NET SDK was not found", run `docker pull mcr.microsoft.com/dotnet/sdk:10.0` to satisfy the `global.json` minimum.

## Release notes

- Write each entry in `docs/changelog/*.md` for the users, who don't know the internals. State what was added, changed or fixed as they would notice it (the behavior or symptom, the feature, the public types and settings, linked with `<xref:...>`), not how it was implemented. Leave out the internal names and mechanisms (e.g. sequences, rollbacks, pushes, internal overloads or helper classes), even when the fix was about them.
- Keep each entry short and telegraphic, usually a single line, but longer when the extra detail is useful to the users. Mention the consequence of a bug when it helps the users recognize it (e.g. "which delayed the reassignment of its partitions until the session timeout"), but do not explain why or how it happened, and do not turn the entry into a guide: the entry is only a hint, optionally pointing to the resolved issue or to the docs page with the details.
- A breaking change states what changed and what the users should check or update. When it's actually the fix of a documented behavior, say so.
- Link the related GitHub issue (e.g. `[[#272](https://github.com/BEagle1984/silverback/issues/272)]`), not the pull request, unless the pull request itself carries significant information. Add no link when there is no issue.
- Entries are grouped by area (e.g. Kafka, mocked Kafka and testing, MQTT, locks), not in chronological order. Insert a new entry next to the existing entries for the same area.
- Do not add entries for changes that only touch tests, such as flaky test fixes.

## Design decisions

- Record design decisions and their rationale in the GitHub issue that tracks the work, and summarize the ones that matter to readers of the code in the PR description. Do not create ADRs unless explicitly asked.

## Git

- Write short Conventional Commits subjects, without articles or filler words: `chore: migrate solutions to slnx`, not `chore: migrate the solutions to the slnx format`.
- When a commit resolves a GitHub issue, end the message with a `Fixes #123` line in the body, so that the issue is linked and closed when the commit reaches `main`. Use `Refs #123` for a commit that only partially addresses the issue.
- Do not commit or push without the user's explicit approval, unless the user asked you to. An approval covers only the change it was given for, not later commits.
