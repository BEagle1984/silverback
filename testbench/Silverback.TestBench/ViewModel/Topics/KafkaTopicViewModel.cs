// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.ViewModel.Topics;

public class KafkaTopicViewModel : TopicViewModel
{
    private readonly ObservableCollection<KafkaPartitionAssignmentEvent> _partitionAssignmentEvents = [];

    private int _partitionsCount;

    public KafkaTopicViewModel(
        string topicName,
        int partitionsCount,
        TimeSpan produceDelay,
        double simulateErrorProbability,
        bool isEnabled = true)
        : base(topicName, produceDelay, simulateErrorProbability, isEnabled)
    {
        _partitionsCount = partitionsCount;

        PartitionAssignmentEvents = new ReadOnlyObservableCollection<KafkaPartitionAssignmentEvent>(_partitionAssignmentEvents);
    }

    public ReadOnlyObservableCollection<KafkaPartitionAssignmentEvent> PartitionAssignmentEvents { get; private set; }

    public int AssignedPartitionsCount =>
        PartitionAssignmentEvents.OfType<KafkaPartitionAssignedEvent>().Count() -
        PartitionAssignmentEvents.OfType<KafkaPartitionRevokedEvent>().Count();

    public int PartitionsCount
    {
        get => _partitionsCount;
        set => SetProperty(ref _partitionsCount, value, nameof(PartitionsCount));
    }

    public void TrackPartitionAssigned(DateTime timestamp, int partition, ContainerInstanceViewModel container)
    {
        lock (_partitionAssignmentEvents)
        {
            _partitionAssignmentEvents.Insert(
                FindAssignmentEventInsertIndex(timestamp),
                new KafkaPartitionAssignedEvent(timestamp, partition, container));
        }

        NotifyPropertyChanged(nameof(AssignedPartitionsCount));
    }

    public void TrackPartitionRevoked(DateTime timestamp, int partition, ContainerInstanceViewModel container)
    {
        lock (_partitionAssignmentEvents)
        {
            _partitionAssignmentEvents.Insert(
                FindAssignmentEventInsertIndex(timestamp),
                new KafkaPartitionRevokedEvent(timestamp, partition, container));
        }

        NotifyPropertyChanged(nameof(AssignedPartitionsCount));
    }

    private int FindAssignmentEventInsertIndex(DateTime timestamp) =>
        _partitionAssignmentEvents
            .TakeWhile(assignmentEvent => assignmentEvent.Timestamp <= timestamp)
            .Count();
}
