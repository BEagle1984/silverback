// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.ViewModel.Topics;

public record KafkaPartitionRevokedEvent(DateTime Timestamp, int Partition, ContainerInstanceViewModel Consumer)
    : KafkaPartitionAssignmentEvent(Timestamp, Partition, Consumer);
