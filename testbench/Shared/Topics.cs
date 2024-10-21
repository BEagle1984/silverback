// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.TestBench;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Constants only")]
public static class Topics
{
    public static class Kafka
    {
        [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Reviewed")]
        public const string Single = "testbench-single";

        public const string Batch = "testbench-batch";

        public const string Unbounded = "testbench-unbounded";
    }

    public static class Mqtt
    {
        public const string Topic1 = "testbench/topic1";

        public const string Topic2 = "testbench/topic2";
    }
}
