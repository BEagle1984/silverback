// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.CodeAnalysis;

namespace Silverback.Tools.Generators.LogEvents;

// A record, because the incremental pipeline compares the models by value to skip the unchanged ones. The location is set only
// for an invalid definition, since it changes with every edit
internal sealed record LogEventModel(string Name, Location? Location = null)
{
    public string Namespace { get; init; } = "";

    public string ClassName { get; init; } = "";

    public int EventId { get; init; }

    public string EventName { get; init; } = "";

    public int Level { get; init; }

    public string Message { get; init; } = "";

    public string Documentation { get; init; } = "";

    public string Usings { get; init; } = "";
}
