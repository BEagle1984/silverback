// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class OutboundLoggerSubstitute<TCategory> : IOutboundLogger<TCategory>
    {
        public ILogger InnerLogger { get; } = Substitute.For<ILogger>();

        public bool IsEnabled(LogEvent logEvent) => true;

        public void LogProduced(IOutboundEnvelope envelope)
        {
        }

        public void LogWrittenToOutbox(IOutboundEnvelope envelope)
        {
        }

        public void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception)
        {
        }
    }
}
