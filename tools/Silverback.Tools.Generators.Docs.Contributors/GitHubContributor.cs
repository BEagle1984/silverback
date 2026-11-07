// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Tools.Generators.Docs.Contributors;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the JSON deserializer")]
internal sealed record GitHubContributor(string Login, string AvatarUrl, string HtmlUrl, string Type);
