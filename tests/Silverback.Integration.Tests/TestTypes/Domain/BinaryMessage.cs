// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.Integration.TestTypes.Domain
{
    public class BinaryMessage
    {
        public Guid MessageId { get; set; }
        public byte[] Content { get; set; }
    }
}
