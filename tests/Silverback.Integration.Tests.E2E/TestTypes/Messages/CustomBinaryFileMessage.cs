// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages;

public class CustomBinaryFileMessage : BinaryFileMessage
{
    [KafkaKeyMember]
    public int? KafkaKey { get; set; }

    [Header("x-custom-header")]
    public string? CustomHeader { get; set; }
}
