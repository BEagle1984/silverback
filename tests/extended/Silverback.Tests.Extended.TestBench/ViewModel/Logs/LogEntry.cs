// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Tests.Extended.TestBench.ViewModel.Containers;

namespace Silverback.Tests.Extended.TestBench.ViewModel.Logs;

public record LogEntry(
    DateTime Timestamp,
    string Message,
    ContainerInstanceViewModel? Container,
    LogLevel Level = LogLevel.Information);
