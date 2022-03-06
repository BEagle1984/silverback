﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages;

public class BinaryMessageWithKafkaKey : BinaryMessage
{
    [KafkaKeyMember]
    public int? KafkaKey { get; set; }
}
