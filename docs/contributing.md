---
uid: contributing
---

# Contributing

You are encouraged to contribute to Silverback! Please check out the [how to contribute](https://github.com/BEagle1984/silverback/blob/develop/CONTRIBUTING.md) guide for guidelines about how to proceed.

## Source Code

The full source code is available on [GitHub](https://github.com/BEagle1984/silverback/)

### Versioning

The [Directory.Build.props](https://github.com/BEagle1984/silverback/blob/master/Directory.Build.props) file in the root of the repository contains the current version of the NuGet packages being built and referenced. The `<BaseVersion>` and `<VersionSuffix>` tags can be used to increment the current version.

### Commits

Please try to follow the [Conventional Commits](https://www.conventionalcommits.org/) specification for the commit messages. 

### Building (NuGet packages)

The nuget packages can be built locally using the powershell script under `/nuget/Update.ps1`. Add the `-l` switch to clear the local nuget cache as well (especially useful when building the same version over and over).

### Testing

The main solution contains quite a few unit tests and additionally some [samples](xref:samples) are implemented in a separate solution.

## Contributors

Thank you to all the present and future [contributors](https://leereilly.net/github-high-scores/?beagle1984/silverback). You are amazing!

<iframe src="https://leereilly.net/github-high-scores/?beagle1984/silverback" style="width: 100%;height: 1000px"></iframe>
