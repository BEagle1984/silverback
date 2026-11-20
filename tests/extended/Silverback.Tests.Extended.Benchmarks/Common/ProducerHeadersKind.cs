// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Extended.Benchmarks.Common;

/// <summary>
///     The headers added by the producer endpoint.
/// </summary>
public enum ProducerHeadersKind
{
    None,

    /// <summary>
    ///     Five headers with a fixed value.
    /// </summary>
    FiveStatic,

    /// <summary>
    ///     Five headers whose value is returned by a function invoked for each message.
    /// </summary>
    FiveValueProvider,

    /// <summary>
    ///     Ten headers with a fixed value.
    /// </summary>
    TenStatic
}
