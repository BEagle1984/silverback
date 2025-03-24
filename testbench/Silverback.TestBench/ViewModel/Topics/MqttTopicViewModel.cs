// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.ViewModel.Topics;

public class MqttTopicViewModel : TopicViewModel
{
    private readonly ObservableCollection<MqttTopicSubscriptionEvent> _subscriptionEvents = [];

    public MqttTopicViewModel(
        string topicName,
        TimeSpan produceDelay,
        double simulateErrorProbability,
        bool isEnabled = true)
        : base(topicName, produceDelay, simulateErrorProbability, isEnabled)
    {
        SubscriptionEvents = new ReadOnlyObservableCollection<MqttTopicSubscriptionEvent>(_subscriptionEvents);
    }

    public ReadOnlyObservableCollection<MqttTopicSubscriptionEvent> SubscriptionEvents { get; private set; }

    public int SubscriptionsCount =>
        SubscriptionEvents.OfType<MqttTopicSubscibedEvent>().Count() -
        SubscriptionEvents.OfType<MqttTopicUnsubscribedEvent>().Count();

    public void TrackSubscribed(DateTime timestamp, ContainerInstanceViewModel container)
    {
        lock (_subscriptionEvents)
        {
            _subscriptionEvents.Insert(FindSubscriptionEventInsertIndex(timestamp), new MqttTopicSubscibedEvent(timestamp, container));
        }

        NotifyPropertyChanged(nameof(SubscriptionsCount));
    }

    public void TrackUnsubscribed(DateTime timestamp, ContainerInstanceViewModel container)
    {
        lock (_subscriptionEvents)
        {
            _subscriptionEvents.Insert(FindSubscriptionEventInsertIndex(timestamp), new MqttTopicUnsubscribedEvent(timestamp, container));
        }

        NotifyPropertyChanged(nameof(SubscriptionsCount));
    }

    private int FindSubscriptionEventInsertIndex(DateTime timestamp)
    {
        for (int i = 0; i < _subscriptionEvents.Count; i++)
        {
            if (_subscriptionEvents[i].Timestamp > timestamp)
                return i;
        }

        return _subscriptionEvents.Count;
    }
}
