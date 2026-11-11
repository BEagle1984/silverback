// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

// Tracks the subscriptions of a consumer, which subscribes again after each reconnection. Wait for the subscriptions through this
// tracker rather than through the Subscribed event, since a message published right after the subscription can still be dropped
// until the subscription has reached all the cluster nodes (see MqttFixture.WaitUntilSubscriptionsPropagatedAsync).
internal sealed class SubscriptionTracker : IDisposable
{
    private readonly MqttConsumer _consumer;

    private readonly System.Threading.Lock _lock = new();

    private readonly List<(int Count, TaskCompletionSource Source)> _waiters = [];

    private int _count;

    // Create the tracker before connecting the consumer, since it subscribes right after the connection
    public SubscriptionTracker(MqttConsumer consumer)
    {
        _consumer = consumer;
        _consumer.Client.Subscribed.AddHandler(OnSubscribedAsync);
    }

    // The number of times the consumer subscribed
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    // Waits until the consumer subscribed the specified number of times in total, then until its subscriptions reached all the nodes
    public async Task WaitUntilSubscribedAsync(int count, TimeSpan timeout)
    {
        Task subscribed;

        lock (_lock)
        {
            if (_count >= count)
            {
                subscribed = Task.CompletedTask;
            }
            else
            {
                TaskCompletionSource source = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Add((count, source));
                subscribed = source.Task;
            }
        }

        try
        {
            await subscribed.WaitAsync(timeout);
        }
        catch (TimeoutException exception)
        {
            // The client doesn't report the subscription if the broker rejected all its topic filters
            throw new TimeoutException(
                $"{_consumer.Configuration.ClientId} subscribed {Count} times instead of {count} within {timeout}.",
                exception);
        }

        await MqttFixture.WaitUntilSubscriptionsPropagatedAsync(
            _consumer.Configuration.ClientId,
            _consumer.Client.SubscribedTopicsFilters.Select(topicFilter => topicFilter.Topic));
    }

    public void Dispose() => _consumer.Client.Subscribed.RemoveHandler(OnSubscribedAsync);

    // Invoked by the connection loop of the client, so it mustn't wait for the propagation itself
    private ValueTask OnSubscribedAsync(BrokerClient client)
    {
        lock (_lock)
        {
            _count++;

            foreach ((int Count, TaskCompletionSource Source) waiter in _waiters.Where(waiter => waiter.Count <= _count))
            {
                waiter.Source.TrySetResult();
            }

            _waiters.RemoveAll(waiter => waiter.Count <= _count);
        }

        return ValueTask.CompletedTask;
    }
}
