// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Tests.Extended.TestBench.ViewModel.Logs;

namespace Silverback.Tests.Extended.TestBench.ViewModel.Trace;

public record MessageTraceEntry(DateTime Timestamp, MessageTraceStatus Status, LogEntry? LogEntry);
