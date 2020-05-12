// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestErrorPolicy : ErrorPolicyBase
    {
        public TestErrorPolicy(IServiceProvider serviceProvider = null)
            : base(serviceProvider, NullLoggerFactory.Instance.CreateLogger<TestErrorPolicy>())
        {
        }

        public bool Applied { get; private set; }

        protected override Task<ErrorAction> ApplyPolicy(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception)
        {
            Applied = true;
            return Task.FromResult(ErrorAction.Skip);
        }
    }
}
