using System;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Repositories;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that subscribes to the message broker and forwards the messages to the internal bus.
    /// This implementation logs the incoming messages and prevents duplicated processing of the same message.
    /// </summary>
    /// <seealso cref="InboundAdapter" />
    public class LoggedInboundAdapter : InboundAdapter
    {
        private readonly IInboundLog _inboundLog;
        private ILogger _logger;

        public LoggedInboundAdapter(IInboundLog inboundLog)
        {
            _inboundLog = inboundLog;
        }

        public override void Init(IBus bus, IEndpoint endpoint, IErrorPolicy errorPolicy = null)
        {
            _logger = bus.GetLoggerFactory().CreateLogger<LoggedInboundAdapter>();
            base.Init(bus, endpoint, errorPolicy);
        }

        /// <summary>
        /// Relays the message ensuring that it wasn't processed already by this microservice.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void RelayMessage(IIntegrationMessage message)
        {
            if (_inboundLog.Exists(message, Endpoint))
            {
                _logger.LogInformation($"Message '{message.Id}' is being skipped since it was already processed.");
                return;
            }

            _inboundLog.Add(message, Endpoint);

            try
            {
                base.RelayMessage(message);
                _inboundLog.Commit();
            }
            catch (Exception)
            {
                _inboundLog.Rollback();
                throw;
            }
        }
    }
}