---
uid: contributing
---

# Contributing

Contributions are welcome. Start with the upstream contribution guidelines:

- https://github.com/BEagle1984/silverback/blob/master/CONTRIBUTING.md

## Source Code

Repository:

- https://github.com/BEagle1984/silverback/

## Versioning

`Directory.Build.props` contains the current NuGet package version. Update:

- `<BaseVersion>`
- `<VersionSuffix>`

## Commits

Please follow the [Conventional Commits](https://www.conventionalcommits.org/) specification.

## Building NuGet Packages

Packages can be built locally using `/nuget/Update.ps1`.

Use `-l` to clear the local NuGet cache (useful when rebuilding the same version).

## Testing

The main solution contains unit tests. There are also runnable <xref:samples> in a separate solution.

## Contributors

Thanks to all contributors [contributors](https://leereilly.net/github-high-scores/?beagle1984/silverback).

<iframe src="https://leereilly.net/github-high-scores/?beagle1984/silverback" style="width: 100%;height: 1000px"></iframe>
