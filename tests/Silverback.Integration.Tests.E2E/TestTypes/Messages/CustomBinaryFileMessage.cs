// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages
{
    public class CustomBinaryFileMessage : BinaryFileMessage
    {
        [Header("x-custom-header")]
        public string? CustomHeader { get; set; }
    }
}
