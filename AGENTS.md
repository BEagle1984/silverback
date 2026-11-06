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
- Name internal implementation methods with the `Core` suffix, before `Async` when applicable: `Stop` -> `StopCore`, `StopAsync` -> `StopCoreAsync`.
- Synchronize shared state with `lock`, `SemaphoreSlim`, `Interlocked` or tasks. Do not use `volatile` or `Volatile.Read`/`Volatile.Write` "just in case" or for weakly ordered CPUs: use them only where the code relies on the order of reads or writes of different fields without a lock, or on a loop without calls seeing a new value, and name that order in a comment.
- Before considering a change done, check the changed C# files for the Rider inspection warnings, which the build does not report: `dotnet jb inspectcode Silverback.slnx --no-build --include="<file>;<file>" --severity=WARNING --format=Text --output=<report>`, with the report in the operating system's temporary directory (it takes about 1.5 minutes). Fix the warnings, or suppress false positives with `[SuppressMessage("ReSharper", "<InspectionId>", Justification = "...")]`, as the surrounding code does.

## Test organization

- Unit tests have one test class per production type, named `<ProductionType>Tests`, in the corresponding unit test project. Mirror the production type's relative folder and namespace beneath the test project's root namespace. Omit generic arity from the test class name, following the existing convention.
- Add coverage to the existing test class instead of introducing a separate feature- or scenario-named class. Only split unusually large classes into partial files when needed; retain the same class and namespace, using filenames such as `<ProductionType>Tests.Feature.cs`.
- E2E and extended integration tests may be organized by feature or use case. This exception does not apply to unit test projects, including unit tests that exercise several collaborating components.

## Running tests

- "Run the tests" means the main solution: `dotnet test Silverback.slnx`. The Docker-dependent tests need the root `docker-compose.yaml` stack running.
- Do not run the extended integration tests (`tests/extended`) unless the user asks for them. Offer them when a change affects broker client behavior, such as consumer or producer logic, rebalancing, or a Confluent.Kafka or MQTTnet upgrade. A run takes over 12 minutes and builds Docker images from the working tree, so do not edit files while it runs.
- The extended tests build with the locally cached `mcr.microsoft.com/dotnet/sdk:10.0` image. If the build fails with "A compatible .NET SDK was not found", run `docker pull mcr.microsoft.com/dotnet/sdk:10.0` to satisfy the `global.json` minimum.

## Release notes

- Keep each entry in `docs/changelog/*.md` to one short line stating what changed or was fixed. Do not explain why or how, and do not turn it into a guide: the entry is only a hint, optionally pointing to the resolved issue or to the docs page with the details.
- Link the related GitHub issue (e.g. `[[#272](https://github.com/BEagle1984/silverback/issues/272)]`), not the pull request, unless the pull request itself carries significant information. Add no link when there is no issue.
- Entries are grouped by area (e.g. Kafka, mocked Kafka and testing, MQTT, locks), not in chronological order. Insert a new entry next to the existing entries for the same area.
- Do not add entries for changes that only touch tests, such as flaky test fixes.

## Git

- Write short Conventional Commits subjects, without articles or filler words: `chore: migrate solutions to slnx`, not `chore: migrate the solutions to the slnx format`.
- Do not commit or push without the user's explicit approval, unless the user asked you to. An approval covers only the change it was given for, not later commits.
