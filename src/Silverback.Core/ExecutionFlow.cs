// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback;

/// <summary>
///     Used as parameter for the internal methods that can run both sync or async.
/// </summary>
internal enum ExecutionFlow
{
    Sync,

    Async
}
