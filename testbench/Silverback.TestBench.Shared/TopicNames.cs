// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.TestBench;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Constants only")]
public static class TopicNames
{
    public static class Kafka
    {
        [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Reviewed")]
        public const string Single = "testbench-kafka-single";

        public const string Batch = "testbench-kafka-batch";

        public const string Unbounded = "testbench-kafka-unbounded";
    }

    public static class Mqtt
    {
        [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Reviewed")]
        public const string Single = "testbench/mqtt/single";

        public const string Unbounded = "testbench/mqtt/unbounded";
    }
}
