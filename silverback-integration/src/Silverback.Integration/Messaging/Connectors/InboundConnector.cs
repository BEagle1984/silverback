using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public class InboundConnector : IInboundConnector
    {
        private readonly IBroker _broker;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<InboundConnector> _logger;

        public InboundConnector(IBroker broker, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            _logger = _loggerFactory.CreateLogger<InboundConnector>();
        }

        public virtual IInboundConnector Bind(IEndpoint endpoint, Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null)
        {
            _logger.LogTrace($"Connecting to inbound endpoint '{endpoint.Name}'...");

            var errorPolicy = errorPolicyFactory?.Invoke(new ErrorPolicyBuilder(_serviceProvider, _loggerFactory)) ?? NoErrorPolicy.Instance;

            // TODO: Carefully test with multiple endpoints!
            // TODO: Test if consumer gets properly disposed etc.
            var consumer = _broker.GetConsumer(endpoint);
            consumer.Received += (sender, envelope) => OnMessageReceived(sender, envelope, endpoint, errorPolicy);
            return this;
        }

        protected void OnMessageReceived(object sender, IEnvelope envelope, IEndpoint endpoint,
            IErrorPolicy errorPolicy) =>
            errorPolicy.TryHandleMessage(
                envelope,
                e => RelayMessage(e.Message, endpoint));

        protected virtual void RelayMessage(IIntegrationMessage message, IEndpoint sourceEndpoint)
        {
            _logger.LogTrace($"Relaying message '{message.Id}' ('{message.GetType().Name}') to the internal bus...");

            using (var scope = _serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(message);
            }
        }
    }
}