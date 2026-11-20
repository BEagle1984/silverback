// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Extended.Benchmarks.Common;

/// <summary>
///     The message produced and consumed by the broker benchmarks: its JSON representation is about 200 bytes with
///     <see cref="SmallPayloadLength" /> and about 64 KB with <see cref="LargePayloadLength" />.
/// </summary>
public sealed record BenchmarkMessage(string Id, string Name, string Payload)
{
    public const int SmallPayloadLength = 100;

    public const int LargePayloadLength = 64 * 1024;

    public static BenchmarkMessage Create(int payloadLength) =>
        new("0f8fad5bd9cb469fa16570867728950e", "Benchmark message", new string('x', payloadLength));
}
