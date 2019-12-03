using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    public abstract class DiagnosticsProducer : Producer
    {
        protected DiagnosticsProducer(IBroker broker, IEndpoint endpoint, MessageKeyProvider messageKeyProvider, ILogger<Producer> logger, MessageLogger messageLogger)
            : base(broker, endpoint, messageKeyProvider, logger, messageLogger)
        {
        }

        // Notice that this is the same code as in "InternalProduceAsync".
        // https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/HttpHandlerDiagnosticListener.cs#L592
        protected override void InternalProduce(OutboundMessage outboundMessage)
        {
            ActivityScope.ExecuteSend(
                DiagnosticsConstants.ActivityNameMessageProducing,
                activity => activity.FillActivity(outboundMessage),
                () => base.InternalProduce(outboundMessage));
        }

        // Notice that this is the same code as in "InternalProduce".
        // https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/HttpHandlerDiagnosticListener.cs#L592
        protected override async Task InternalProduceAsync(OutboundMessage outboundMessage)
        {
            await ActivityScope.ExecuteSendAsync(
                DiagnosticsConstants.ActivityNameMessageProducing,
                activity => activity.FillActivity(outboundMessage),
                () => base.InternalProduceAsync(outboundMessage));
        }
    }

    public abstract class DiagnosticsProducer<TBroker, TEndpoint> : DiagnosticsProducer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected DiagnosticsProducer(IBroker broker, IEndpoint endpoint, MessageKeyProvider messageKeyProvider,
            ILogger<Producer> logger, MessageLogger messageLogger)
            : base(broker, endpoint, messageKeyProvider, logger, messageLogger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}
