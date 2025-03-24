// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.TestBench.ViewModel.Trace;

public enum MessageTraceStatus
{
    Produced,

    Processing,

    ProcessingError,

    Lost,

    Processed,
}
