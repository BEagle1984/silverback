---
title: Contributing
permalink: /docs/source/contributing
toc: true
---

You are encouraged to contribute to Silverback! Please check out the [how to contribute](https://github.com/BEagle1984/silverback/blob/develop/CONTRIBUTING.md) guide for guidelines about how to proceed.

## Repository

The full source code is available on [GitHub](https://github.com/BEagle1984/silverback/)

## Building

### Versioning

The `Directory.Build.props` file in the root of the repository contains the current version of the nuget packages being built (and referenced). The `<BaseVersion>` and `<VersionSuffix>` tags can be used to increment the current version.

### Nuget packages

The nuget packages can be built locally using the powershell scipt under `/nuget/Update.ps1`. Add the `-l` switch to clear the local nuget cache as well (especially useful when building the same verison over and over).

## Testing

The main solution contains quite a few unit tests and additionally some [samples]({{ site.baseurl }}/docs/source/samples) are implemented in a separate solution.



