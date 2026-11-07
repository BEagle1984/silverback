// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Silverback.Tools.Generators.Docs.Contributors;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    await Console.Error.WriteLineAsync("Usage: Silverback.Tools.Generators.Docs.Contributors <output directory>").ConfigureAwait(false);
    return 1;
}

string outputDirectory = args[0];
Directory.CreateDirectory(outputDirectory);

await DocsGenerator.GenerateContributorsGridAsync(Path.Combine(outputDirectory, "contributors.md")).ConfigureAwait(false);

return 0;
