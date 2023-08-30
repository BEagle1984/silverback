// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Models;

public class TestBenchMessage
{
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public string MessageId { get; init; } = Guid.NewGuid().ToString("N");

    public TimeSpan SimulatedProcessingTime { get; init; } = TimeSpan.FromMilliseconds(42);

    public int SimulatedFailuresCount { get; init; }
}
