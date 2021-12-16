// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestErrorPolicy : RetryableErrorPolicyBase
    {
        private TestErrorPolicyImplementation? _implementation;

        public bool Applied => _implementation!.Applied;

        protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider)
        {
            _implementation ??= new TestErrorPolicyImplementation(
                MaxFailedAttemptsCount,
                ExcludedExceptions,
                IncludedExceptions,
                ApplyRule,
                MessageToPublishFactory,
                serviceProvider,
                Substitute.For<IInboundLogger<TestErrorPolicy>>());

            return _implementation;
        }

        private sealed class TestErrorPolicyImplementation : ErrorPolicyImplementation
        {
            public TestErrorPolicyImplementation(
                int? maxFailedAttempts,
                ICollection<Type> excludedExceptions,
                ICollection<Type> includedExceptions,
                Func<IRawInboundEnvelope, Exception, bool>? applyRule,
                Func<IRawInboundEnvelope, object?>? messageToPublishFactory,
                IServiceProvider serviceProvider,
                IInboundLogger<TestErrorPolicy> logger)
                : base(
                    maxFailedAttempts,
                    excludedExceptions,
                    includedExceptions,
                    applyRule,
                    messageToPublishFactory,
                    serviceProvider,
                    logger)
            {
            }

            public bool Applied { get; private set; }

            protected override Task<bool> ApplyPolicyAsync(
                ConsumerPipelineContext context,
                Exception exception)
            {
                Applied = true;
                return Task.FromResult(false);
            }
        }
    }
}
