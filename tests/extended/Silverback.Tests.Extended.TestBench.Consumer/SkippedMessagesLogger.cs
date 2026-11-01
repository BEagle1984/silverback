// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.Shared.Messages;

namespace Silverback.Tests.Extended.TestBench.Consumer;

public class SkippedMessagesLogger : ISkipMessageErrorPolicyCallback
{
    private readonly ILogger<SkippedMessagesLogger> _logger;

    public SkippedMessagesLogger(ILogger<SkippedMessagesLogger> logger)
    {
        _logger = logger;
    }

    public Task OnSkippingMessageAsync(IRawInboundEnvelope envelope, Exception exception)
    {
        if (envelope is IInboundEnvelope { Message: TestBenchMessage message })
        {
            _logger.LogInformation(
                "Message {MessageId} from topic {TopicName} skipped via error policy",
                message.MessageId,
                envelope.Endpoint.RawName);
        }

        return Task.CompletedTask;
    }
}
