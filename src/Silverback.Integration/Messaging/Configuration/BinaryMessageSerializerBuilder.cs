// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="BinaryMessageSerializer" />.
/// </summary>
public sealed class BinaryMessageSerializerBuilder
{
    /// <summary>
    ///     Builds the <see cref="IMessageSerializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageSerializer" />.
    /// </returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Ready for extension")]
    public IMessageSerializer Build() => new BinaryMessageSerializer();
}
