// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Tests.Extended.Benchmarks.VersionComparison.V5_9_0.MockedBroker;

/// <summary>
///     The message produced and consumed by the mocked broker benchmarks: its JSON representation is about 200 bytes.
/// </summary>
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "Serialized as JSON")]
public sealed record SampleMessage(string Id, string Name, string Payload)
{
    public static SampleMessage Create() => new("0f8fad5bd9cb469fa16570867728950e", "Benchmark message", new string('x', 100));
}
