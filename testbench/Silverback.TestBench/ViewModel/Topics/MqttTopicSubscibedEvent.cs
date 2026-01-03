// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.ViewModel.Topics;

public record MqttTopicSubscibedEvent(DateTime Timestamp, ContainerInstanceViewModel Container) : MqttTopicSubscriptionEvent(Timestamp, Container);
