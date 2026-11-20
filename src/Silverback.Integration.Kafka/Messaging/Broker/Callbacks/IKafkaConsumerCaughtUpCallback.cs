// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnConsumerCaughtUpAsync" /> event handler.
/// </summary>
/// <remarks>
///     The <see cref="KafkaConsumerConfiguration.CatchUpMode" /> must be set, otherwise the consumer doesn't track whether it's
///     caught up.
/// </remarks>
public interface IKafkaConsumerCaughtUpCallback : IBrokerClientCallback
{
    /// <summary>
    ///     Called when the consumer is caught up, meaning that every partition assigned to it reached its end and all its messages are
    ///     processed.
    /// </summary>
    /// <remarks>
    ///     The callback is invoked once per connection: a reconnect starts a new catch-up. In <see cref="CatchUpMode.Disconnect" />
    ///     mode, the consumer is disconnected after the callback returns.
    /// </remarks>
    /// <param name="consumer">
    ///     The related consumer.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task OnConsumerCaughtUpAsync(IKafkaConsumer consumer);
}
