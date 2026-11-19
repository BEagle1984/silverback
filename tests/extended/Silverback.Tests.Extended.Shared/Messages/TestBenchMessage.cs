// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.Extended.Shared.Messages;

public class TestBenchMessage
{
    public DateTime CreatedAt { get; set; }

    public string MessageId { get; set; } = "";

    public TimeSpan SimulatedProcessingTime { get; set; }

    public int SimulatedFailuresCount { get; set; }
}
