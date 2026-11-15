// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Sequences;

// How the timeout of a sequence is measured (see SequenceBase)
internal enum SequenceTimeoutMode
{
    // The timeout restarts with each added message: it elapses when no message is added for the timeout duration (e.g. the next chunk
    // doesn't arrive)
    Sliding,

    // The timeout is measured from the first message (e.g. the max wait time of a batch)
    Absolute
}
