// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Configuration.Models;

public abstract record TopicConfiguration(string TopicName, TimeSpan ProduceDelay = default);
