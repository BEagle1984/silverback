// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.ViewModel.Topics;

public abstract record MqttTopicSubscriptionEvent(DateTime Timestamp, ContainerInstanceViewModel Container);
