---
name: start-release
description: Open a new Silverback release (for example 5.7.0) by bumping the version in Directory.Build.props and adding its chapter at the top of the release notes. Use when asked to start, open or begin a new release or version.
---

# Start a new release

Opening a release touches two files. Work on `master` from the repository root unless told otherwise.

Start with a patch version (5.6.1) when the release begins with a fix, and a minor version (5.7.0) when it begins with a feature. If a feature lands later in a patch release, promote it to the next minor version by repeating both steps on the existing chapter instead of adding a new one.

## 1. Bump the version

In `Directory.Build.props`, set `BaseVersion` to the new version, keeping the `$(BaseVersionSuffix)` reference:

```xml
<BaseVersion>5.7.0$(BaseVersionSuffix)</BaseVersion>
```

Leave `BaseVersionSuffix` as it is unless the user asks for a pre-release (for example `-beta.1`).

If the current version has no release notes date yet (see below), it was never released: ask whether to rename that chapter to the new version instead of adding another one.

## 2. Add the release notes chapter

The release notes live in `docs/changelog/<major>00.md` (`500.md` for 5.x, `600.md` for 6.x). Insert the new chapter directly under the `# Silverback <major> Release Notes` title, above the previous release:

```markdown
## [5.7.0](https://github.com/BEagle1984/silverback/releases/tag/v5.7.0)

```

- Add only the heading. The `### What's New`, `### Fixes` and `### Breaking Changes` sections are added later, together with the first entry that needs them.
- Do not add the `<p class="release-date">...` line: it is added when the release is published.

### New major version

For a new major version, create `docs/changelog/<major>00.md` following the previous file's front matter and title (`uid: releases-<major>00`, `# Silverback <major> Release Notes`), add it at the top of `docs/changelog/toc.yml`, and update the links to the latest release notes (`<xref:releases-<major>00>` in `docs/about.md`). Ask before changing references in the migration guides.

## 3. Commit

Do not create a dedicated commit for opening the release: include these changes in the commit of the first fix or feature of the release.
