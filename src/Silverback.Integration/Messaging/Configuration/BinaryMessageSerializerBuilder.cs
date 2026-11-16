// Copyright (c) 2026 Sergio Aquilini
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
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global", Justification = "Ready for extension")]
    [SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Instance method kept so that the builder can be extended with settings later")]
    public IMessageSerializer Build() => new BinaryMessageSerializer();
}
