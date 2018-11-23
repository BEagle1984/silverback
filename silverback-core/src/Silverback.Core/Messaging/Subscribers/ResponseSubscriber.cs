using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Internally used to handle request/response use case.
    /// </summary>
    internal class ResponseSubscriber : ISubscriber
    {
        public static event EventHandler<IResponse> ResponseReceived;

        [Subscribe]
        public void OnResponsePublished(IResponse response) => ResponseReceived?.Invoke(this, response);
    }
}
