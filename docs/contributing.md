---
uid: contributing
description: "Contribute to Silverback: get the source code, learn how versions are set and commits are written, build the NuGet packages locally and find the tests."
---

# Contributing

Contributions are welcome. Start with the upstream contribution guidelines:

- https://github.com/BEagle1984/silverback/blob/main/CONTRIBUTING.md

## Source Code

Repository:

- https://github.com/BEagle1984/silverback/

## Versioning

`Directory.Build.props` contains the current NuGet package version. Update:

- `<BaseVersion>`
- `<BaseVersionSuffix>`

## Commits

Please follow the [Conventional Commits](https://www.conventionalcommits.org/) specification.

## Building NuGet Packages

Packages can be built locally using `/nuget/pack.ps1`, which writes them to the `/nuget` folder. Rebuilding the same version
replaces the cached copies of the packages, so the freshly built ones are restored.

## Testing

The main solution contains unit tests. There are also runnable <xref:samples> in a separate solution.

## Contributors

Thanks to all the [contributors](https://github.com/BEagle1984/silverback/graphs/contributors)!

[!INCLUDE [Contributors](build/generated/contributors.md)]
