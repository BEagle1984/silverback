// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Models;

public class TestBenchMessage
{
    public DateTime CreatedAt { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public TimeSpan SimulatedProcessingTime { get; set; }

    public int SimulatedFailuresCount { get; set; }
}
