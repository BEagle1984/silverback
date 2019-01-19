// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes
{
    public class TestErrorPolicy : ErrorPolicyBase
    {
        public bool Applied { get; private set; }

        public TestErrorPolicy() : base(NullLoggerFactory.Instance.CreateLogger<TestErrorPolicy>())
        {
        }

        public override ErrorAction HandleError(FailedMessage failedMessage, Exception exception)
        {
            Applied = true;
            return ErrorAction.Skip;
        }
    }
}