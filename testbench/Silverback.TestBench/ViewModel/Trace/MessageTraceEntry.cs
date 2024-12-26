// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.TestBench.ViewModel.Logs;

namespace Silverback.TestBench.ViewModel.Trace;

public record MessageTraceEntry(DateTime Timestamp, MessageTraceStatus Status, LogEntry? LogEntry);
