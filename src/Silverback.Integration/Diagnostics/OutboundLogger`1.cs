// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics
{
    internal class OutboundLogger<TCategoryName>
        : SilverbackLogger<TCategoryName>, IOutboundLogger<TCategoryName>
    {
        private readonly LoggerCollection _loggers;

        public OutboundLogger(IMappedLevelsLogger<TCategoryName> mappedLevelsLogger, LoggerCollection loggers)
            : base(mappedLevelsLogger)
        {
            _loggers = Check.NotNull(loggers, nameof(loggers));
        }

        public void LogProduced(IOutboundEnvelope envelope) =>
            GetLogger(envelope).LogProduced(this, envelope);

        public void LogWrittenToOutbox(IOutboundEnvelope envelope) =>
            GetLogger(envelope).LogWrittenToOutbox(this, envelope);

        public void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception) =>
            GetLogger(envelope).LogErrorProducingOutboxStoredMessage(this, envelope, exception);

        private OutboundLogger GetLogger(IOutboundEnvelope envelope) =>
            _loggers.GetOutboundLogger(envelope.Endpoint.GetType());
    }
}
