# Repository guidance

## Temporary investigation files

- Use the repository's top-level `temp/` directory for human-readable Markdown investigation reports. Keep one current summary per investigation, updating the findings, evidence and status instead of accumulating overlapping reports. Distinguish committed fixes, pending fixes and open issues.
- Give reports meaningful names including the creation date: `YYYY-MM-DD-description.md`, for example `2026-09-18-kafka-rebalance-findings.md`. Keep the filename stable when updating an existing summary; update the date inside it.
- Do not retain disposable editing scripts, scratch projects, build output or duplicate logs in `temp/`. Prefer inline commands or the operating system's temporary directory for one-off helpers; delete any repository-local scratch files before finishing the task.
- Put test logs, TRX files and other generated evidence in the existing test/tool output directories. Retain only evidence needed for unresolved findings or meaningful verification, reference it from the report, and remove redundant outputs. Do not discard an unexplained failure merely because a retry passes.
- `temp/` is ignored by Git. Do not commit or force-add reports, add them as Solution Items, or create permanent documentation links to individual temporary files. Keep maintained documentation in its normal location.

## Code conventions

- Do not add a trailing period to simple, single-sentence code comments.
- Name internal implementation methods with the `Core` suffix, before `Async` when applicable: `Stop` -> `StopCore`, `StopAsync` -> `StopCoreAsync`.
