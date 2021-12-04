// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Performance;

// Baseline
// |                          Method |       Mean |      Error |     StdDev |     Median |   Gen 0 | Gen 1 | Gen 2 | Allocated |
// |-------------------------------- |-----------:|-----------:|-----------:|-----------:|--------:|------:|------:|----------:|
// |           MessagesWithSimpleKey | 256.076 us | 10.2684 us | 28.9623 us | 248.838 us | 20.0000 |     - |     - |     49 KB |
// | MessagesWithSimpleKeyButNoValue | 255.138 us |  8.1419 us | 22.8308 us | 253.428 us | 20.0000 |     - |     - |     49 KB |
// |         MessagesWithCombinedKey | 416.836 us | 14.1230 us | 39.6023 us | 406.556 us | 40.0000 |     - |     - |    100 KB |
// |              MessagesWithoutKey |   1.863 us |  0.1089 us |  0.3107 us |   1.777 us |       - |     - |     - |      2 KB |

// Current version
// |                          Method |      Mean |     Error |    StdDev |   Gen 0 | Gen 1 | Gen 2 | Allocated |
// |-------------------------------- |----------:|----------:|----------:|--------:|------:|------:|----------:|
// |           MessagesWithSimpleKey | 13.955 us | 0.4092 us | 1.1674 us |       - |     - |     - |         - |
// | MessagesWithSimpleKeyButNoValue | 13.471 us | 0.2694 us | 0.7466 us |       - |     - |     - |         - |
// |         MessagesWithCombinedKey | 75.546 us | 1.6008 us | 4.6186 us | 20.9961 |     - |     - |  44,000 B |
// |              MessagesWithoutKey |  2.860 us | 0.0712 us | 0.2043 us |       - |     - |     - |         - |
[SimpleJob]
[MemoryDiagnoser]
public class KafkaKeyHelperBenchmark
{
    private const int MessageCount = 100;

    private readonly MessageWithoutKey[] _messageWithoutKeys = new MessageWithoutKey[MessageCount];

    private readonly MessageWithSimpleKey[] _messageWithSimpleKeys =
        new MessageWithSimpleKey[MessageCount];

    private readonly MessageWitCombinedKey[] _messageWitCombinedKeys =
        new MessageWitCombinedKey[MessageCount];

    public KafkaKeyHelperBenchmark()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _messageWitCombinedKeys[i] = new MessageWitCombinedKey
            {
                MyKey1 = "FirstKey" + i,
                MyKey2 = "SecondKey" + i
            };
            _messageWithSimpleKeys[i] = new MessageWithSimpleKey
            {
                MyKey = "FirstKey" + i
            };
            _messageWithoutKeys[i] = new MessageWithoutKey();
        }
    }

    [Benchmark]
    public void MessagesWithSimpleKey()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _ = KafkaKeyHelper.GetMessageKey(_messageWithSimpleKeys[i]);
        }
    }

    [Benchmark]
    public void MessagesWithSimpleKeyButNoValue()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            MessageWithSimpleKey message = _messageWithSimpleKeys[i];
            message.MyKey = string.Empty;
            _ = KafkaKeyHelper.GetMessageKey(message);
        }
    }

    [Benchmark]
    public void MessagesWithCombinedKey()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _ = KafkaKeyHelper.GetMessageKey(_messageWitCombinedKeys[i]);
        }
    }

    [Benchmark]
    public void MessagesWithoutKey()
    {
        for (int i = 0; i < MessageCount; i++)
        {
            _ = KafkaKeyHelper.GetMessageKey(_messageWithoutKeys[i]);
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Needed for testing purpose")]
    private sealed class MessageWithSimpleKey
    {
        [KafkaKeyMember]
        public string? MyKey { get; set; }

        public string? MyProp1 { get; set; }

        public string? MyProp2 { get; set; }

        public string? MyProp3 { get; set; }

        public string? MyProp4 { get; set; }

        public string? MyProp5 { get; set; }

        public string? MyProp6 { get; set; }

        public string? MyProp7 { get; set; }

        public string? MyProp8 { get; set; }

        public string? MyProp9 { get; set; }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Needed for testing purpose")]
    private sealed class MessageWitCombinedKey
    {
        [KafkaKeyMember]
        public string? MyKey1 { get; set; }

        [KafkaKeyMember]
        public string? MyKey2 { get; set; }

        [KafkaKeyMember]
        public string? MyKey3 { get; set; }

        public string? MyProp1 { get; set; }

        public string? MyProp2 { get; set; }

        public string? MyProp3 { get; set; }

        public string? MyProp4 { get; set; }

        public string? MyProp5 { get; set; }

        public string? MyProp6 { get; set; }

        public string? MyProp7 { get; set; }

        public string? MyProp8 { get; set; }

        public string? MyProp9 { get; set; }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Needed for testing purpose")]
    private sealed class MessageWithoutKey
    {
        public string? MyProp1 { get; set; }

        public string? MyProp2 { get; set; }

        public string? MyProp3 { get; set; }

        public string? MyProp4 { get; set; }

        public string? MyProp5 { get; set; }

        public string? MyProp6 { get; set; }

        public string? MyProp7 { get; set; }

        public string? MyProp8 { get; set; }

        public string? MyProp9 { get; set; }
    }
}
