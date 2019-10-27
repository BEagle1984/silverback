// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestErrorPolicy : ErrorPolicyBase
    {
        public bool Applied { get; private set; }

        public TestErrorPolicy(IServiceProvider serviceProvider = null) : base(serviceProvider, NullLoggerFactory.Instance.CreateLogger<TestErrorPolicy>(), new MessageLogger())
        {
        }

        protected override ErrorAction ApplyPolicy(IEnumerable<IInboundMessage> messages, Exception exception)
        {
            Applied = true;
            return ErrorAction.Skip;
        }
    }
}