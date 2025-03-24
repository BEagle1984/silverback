// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.ViewModel.Logs;

public record LogEntry(
    DateTime Timestamp,
    string Message,
    ContainerInstanceViewModel? Container,
    LogLevel Level = LogLevel.Information);
