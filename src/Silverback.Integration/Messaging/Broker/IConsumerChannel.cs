// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Represents a channel used to consume the messages from a topic (or partition).
/// </summary>
internal interface IConsumerChannel
{
    /// <summary>
    ///     Gets the unique identifier of the channel. Used to identify the channel in the logs.
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     Gets the <see cref="CancellationToken" /> that is cancelled when the reading stops.
    /// </summary>
    CancellationToken ReadCancellationToken { get; }

    /// <summary>
    ///     Gets the <see cref="Task" /> representing the reading process.
    /// </summary>
    Task ReadTask { get; }

    /// <summary>
    ///     Completes the channel, preventing any further writing (until reset).
    /// </summary>
    void Complete();

    /// <summary>
    ///     Resets the channel, allowing further writing.
    /// </summary>
    void Reset();

    /// <summary>
    ///     Starts the reading process.
    /// </summary>
    /// <returns>
    ///     A value indicating whether the reading process has been started. Returns <c>false</c> if the reading process was already started
    ///     before calling this method.
    /// </returns>
    bool StartReading();

    /// <summary>
    ///     Stops the reading process.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task StopReadingAsync();

    /// <summary>
    ///     Completes the <see cref="ReadTask" />, notifying that the reading process has stopped.
    ///     A call to this method will also abort the pending sequences.
    /// </summary>
    /// <param name="hasThrown">
    ///     A value indicating whether the reading process has stopped because of an exception.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task NotifyReadingStoppedAsync(bool hasThrown);
}
