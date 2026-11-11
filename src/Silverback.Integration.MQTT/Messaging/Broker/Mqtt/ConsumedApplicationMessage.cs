// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;

namespace Silverback.Messaging.Broker.Mqtt;

internal sealed class ConsumedApplicationMessage
{
    private readonly MqttApplicationMessageReceivedEventArgs _eventArgs;

    public ConsumedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs, ConsumerChannel channel, long position)
    {
        _eventArgs = eventArgs;
        Channel = channel;
        Position = position;
        Id = Guid.NewGuid().ToString("N");
    }

    public string Id { get; }

    public MqttApplicationMessage ApplicationMessage => _eventArgs.ApplicationMessage;

    /// <summary>
    ///     Gets the channel the message was written to. The message is always processed and replayed by this channel.
    /// </summary>
    public ConsumerChannel Channel { get; }

    /// <summary>
    ///     Gets the position of the message in the order it was received, which is also the order of the messages in its channel.
    /// </summary>
    public long Position { get; }

    /// <summary>
    ///     Gets the task completed when the message is committed (<c>true</c>) or rolled back (<c>false</c>), or faulted when it can't be
    ///     acknowledged. It is replaced every time the message is read again to be replayed.
    /// </summary>
    public TaskCompletionSource<bool> TaskCompletionSource { get; private set; } = CreateTaskCompletionSource();

    /// <summary>
    ///     Gets or sets a value indicating whether the message was rolled back or put back by a stopping reader and must be read again.
    ///     Guarded by the channel lock.
    /// </summary>
    public bool IsReplayPending { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the reader moved on while the message is still pending, because its commit or
    ///     rollback is left to the sequence it was added to (e.g. a batch). Guarded by the channel lock.
    /// </summary>
    public bool IsDeferred { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the message was added to a sequence, which tracks its identifier and therefore commits
    ///     or rolls it back. Guarded by the channel lock.
    /// </summary>
    public bool IsTracked { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the message is being acknowledged, after being committed with its sequence. It's still
    ///     pending until acknowledged, but must not be replayed anymore. Guarded by the channel lock.
    /// </summary>
    public bool IsAcknowledging { get; set; }

    /// <summary>
    ///     Gets or sets the node of the message in the list of the pending messages of its channel, or <c>null</c> if it was acknowledged
    ///     or discarded. Guarded by the channel lock.
    /// </summary>
    public LinkedListNode<ConsumedApplicationMessage>? PendingNode { get; set; }

    /// <summary>
    ///     Gets the number of times the message was read again to be replayed. Guarded by the channel lock.
    /// </summary>
    public int ReplayCount { get; private set; }

    public Task AcknowledgeAsync(CancellationToken cancellationToken) => _eventArgs.AcknowledgeAsync(cancellationToken);

    /// <summary>
    ///     Prepares the message to be processed again. Called under the channel lock.
    /// </summary>
    public void ResetForReplay()
    {
        IsReplayPending = false;
        IsDeferred = false;
        IsTracked = false;
        ReplayCount++;
        TaskCompletionSource = CreateTaskCompletionSource();
    }

    // The continuations must never run under the channel lock, which is held while completing the task
    private static TaskCompletionSource<bool> CreateTaskCompletionSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
