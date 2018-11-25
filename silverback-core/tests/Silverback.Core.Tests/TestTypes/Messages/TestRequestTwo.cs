// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Messages
{
    public class TestRequestTwo : IRequest, ITestMessage
    {
        public Guid RequestId { get; set; }

        public string Message { get; set; }
    }
}