// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.TestBench.Models;

namespace Silverback.TestBench.Producer;

public class RoutableTestBenchMessage : TestBenchMessage
{
    public RoutableTestBenchMessage(string targetTopic)
    {
        TargetTopic = targetTopic;
    }

    public string TargetTopic { get; }
}
