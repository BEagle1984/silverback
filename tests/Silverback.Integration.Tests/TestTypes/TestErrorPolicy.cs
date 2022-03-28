// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes;

public record TestErrorPolicy : ErrorPolicyBase
{
    private TestErrorPolicyImplementation? _implementation;

    public bool Applied => _implementation!.Applied;

    protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider)
    {
        _implementation ??= new TestErrorPolicyImplementation(
            MaxFailedAttempts,
            ExcludedExceptions,
            IncludedExceptions,
            ApplyRule,
            MessageToPublishFactory,
            serviceProvider,
            Substitute.For<IConsumerLogger<TestErrorPolicy>>());

        return _implementation;
    }

    private sealed class TestErrorPolicyImplementation : ErrorPolicyImplementation
    {
        public TestErrorPolicyImplementation(
            int? maxFailedAttempts,
            IReadOnlyCollection<Type> excludedExceptions,
            IReadOnlyCollection<Type> includedExceptions,
            Func<IRawInboundEnvelope, Exception, bool>? applyRule,
            Func<IRawInboundEnvelope, Exception, object?>? messageToPublishFactory,
            IServiceProvider serviceProvider,
            IConsumerLogger<TestErrorPolicy> logger)
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
