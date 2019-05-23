namespace Silverback.Messaging.Diagnostics
{
    public class DiagnosticsConstants
    {
        public const string ActivityNameMessageConsuming = "Silverback.Messaging.Broker.Consumer.ConsumeMessage";

        public const string ActivityNameMessageProducing = "Silverback.Messaging.Broker.Producer.ProduceMessage";

        public const string CorrelationIdHeaderKey = "CorrelationId"; // TODO: Config and share with consumer

        public const string CorrelationBaggageHeaderKey = "Correlation-Context"; // TODO: Config and share with consumer

        public const string DiagnosticListenerNameProducer = "Silverback.Producer";

        public const string DiagnosticListenerNameConsumer = "Silverback.Consumer";
    }
}
