// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.TestBench.ViewModel.Trace;

public enum MessageTraceStatusFilter
{
    Pending,

    NotConsumed,

    Error,

    Processed,

    Any
}
