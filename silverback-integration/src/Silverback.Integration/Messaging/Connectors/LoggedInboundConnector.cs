using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// This implementation logs the incoming messages and prevents duplicated processing of the same message.
    /// </summary>
    public class LoggedInboundConnector : InboundConnector
    {
        private readonly IInboundLog _inboundLog;
        private ILogger _logger;

        public LoggedInboundConnector(IBroker broker, IServiceProvider serviceProvider, IInboundLog inboundLog, ILoggerFactory loggerFactory)
            : base(broker, serviceProvider, loggerFactory)
        {
            _inboundLog = inboundLog;
            _logger = loggerFactory.CreateLogger<LoggedInboundConnector>();
        }

        protected override void RelayMessage(IIntegrationMessage message, IEndpoint sourceEndpoint)
        {
            if (_inboundLog.Exists(message, sourceEndpoint))
            {
                _logger.LogInformation($"Message '{message.Id}' is being skipped since it was already processed.");
                return;
            }

            _inboundLog.Add(message, sourceEndpoint);

            try
            {
                base.RelayMessage(message, sourceEndpoint);
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