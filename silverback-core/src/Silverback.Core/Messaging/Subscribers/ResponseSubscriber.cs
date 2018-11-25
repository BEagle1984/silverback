// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
