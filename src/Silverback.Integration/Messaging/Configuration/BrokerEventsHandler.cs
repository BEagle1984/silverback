using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Basic class that provides empty implementation for the <see cref="IBrokerEventsHandler"/>.
    /// </summary>
    public class BrokerEventsHandler : IBrokerEventsHandler
    {
        /// <inheritdoc cref="IBrokerEventsHandler"/>
        public virtual Task OnEndpointsConfiguredAsync() => Task.CompletedTask;
    }
}
