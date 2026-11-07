// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.CodeAnalysis;

namespace Silverback.Tools.Generators.LogEvents;

// A record, because the incremental pipeline compares the models by value to skip the unchanged ones. The location is set only
// for an invalid definition, since it changes with every edit
internal sealed record LogEventModel(string Name, Location? Location = null)
{
    public string Namespace { get; init; } = string.Empty;

    public string ClassName { get; init; } = string.Empty;

    public int EventId { get; init; }

    public string EventName { get; init; } = string.Empty;

    public int Level { get; init; }

    public string Message { get; init; } = string.Empty;

    public string Documentation { get; init; } = string.Empty;

    public string Usings { get; init; } = string.Empty;
}
