using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    public abstract class DiagnosticsConsumer : Consumer
    {
        protected DiagnosticsConsumer(IBroker broker, IEndpoint endpoint)
            : base(broker, endpoint)
        {
        }

        protected override async Task HandleMessage(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset)
        {
            var messageHeaders = headers.ToList();

            await ActivityScope.ExecuteReceiveAsync(
                DiagnosticsConstants.ActivityNameMessageConsuming,
                activity => activity.FillActivity(messageHeaders),
                () => base.HandleMessage(message, messageHeaders, offset));
        }
    }


    public abstract class DiagnosticsConsumer<TBroker, TEndpoint> : DiagnosticsConsumer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected DiagnosticsConsumer(IBroker broker, IEndpoint endpoint)
            : base(broker, endpoint)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}
