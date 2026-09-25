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

## Test organization

- Unit tests have one test class per production type, named `<ProductionType>Tests`, in the corresponding unit test project. Mirror the production type's relative folder and namespace beneath the test project's root namespace. Omit generic arity from the test class name, following the existing convention.
- Add coverage to the existing test class instead of introducing a separate feature- or scenario-named class. Only split unusually large classes into partial files when needed; retain the same class and namespace, using filenames such as `<ProductionType>Tests.Feature.cs`.
- E2E and extended integration tests may be organized by feature or use case. This exception does not apply to unit test projects, including unit tests that exercise several collaborating components.
