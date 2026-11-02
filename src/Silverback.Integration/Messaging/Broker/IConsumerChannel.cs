// Copyright (c) 2026 Sergio Aquilini
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
    ///     Gets the <see cref="CancellationToken" /> that is canceled when the reading stops.
    /// </summary>
    CancellationToken ReadCancellationToken { get; }

    /// <summary>
    ///     Gets the <see cref="Task" /> representing the reading process, which completes after the pending sequences are aborted.
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
    ///     before calling this method, including while the previous reading process is still aborting its pending sequences (see
    ///     <see cref="NotifyReadingStoppedAsync" />). The reading can be restarted after the <see cref="ReadTask" /> completes.
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
    ///     Aborts the pending sequences, awaiting their processing, and then completes the <see cref="ReadTask" />, notifying that the
    ///     reading process has stopped.
    /// </summary>
    /// <param name="hasThrown">
    ///     A value indicating whether the reading process has stopped because of an exception.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task NotifyReadingStoppedAsync(bool hasThrown);
}
