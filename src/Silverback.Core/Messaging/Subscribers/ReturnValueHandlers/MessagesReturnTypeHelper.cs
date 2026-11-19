// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Determines whether the values of a type are the messages to be republished by the built-in <see cref="IReturnValueHandler" />
///     implementations, according to the configured message types. The type is either the runtime type of a returned value or the
///     result type declared by a subscriber.
/// </summary>
internal static class MessagesReturnTypeHelper
{
    /// <summary>
    ///     Returns a value indicating whether the values of the specified type are messages.
    /// </summary>
    /// <param name="type">
    ///     The type to be checked.
    /// </param>
    /// <param name="messageTypes">
    ///     The types to be handled as messages.
    /// </param>
    /// <returns>
    ///     A value indicating whether the values of the specified type are messages.
    /// </returns>
    public static bool IsMessage(Type type, IEnumerable<Type> messageTypes)
    {
        // A boxed Nullable<T> is a boxed T
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return messageTypes.Any(messageType => messageType.IsAssignableFrom(underlyingType));
    }

    /// <summary>
    ///     Returns a value indicating whether the values of the specified type implement the specified generic interface (e.g.
    ///     <see cref="IEnumerable{T}" />) of a message type.
    /// </summary>
    /// <param name="type">
    ///     The type to be checked.
    /// </param>
    /// <param name="genericInterfaceDefinition">
    ///     The generic type definition of the interface wrapping the messages (e.g. <c>typeof(IEnumerable&lt;&gt;)</c>).
    /// </param>
    /// <param name="messageTypes">
    ///     The types to be handled as messages.
    /// </param>
    /// <returns>
    ///     A value indicating whether the values of the specified type wrap the messages.
    /// </returns>
    public static bool IsMessagesCollection(Type type, Type genericInterfaceDefinition, IEnumerable<Type> messageTypes) =>
        (type.IsInterface ? type.GetInterfaces().Prepend(type) : type.GetInterfaces())
        .Any(interfaceType => interfaceType.IsGenericType &&
                              interfaceType.GetGenericTypeDefinition() == genericInterfaceDefinition &&
                              messageTypes.Any(messageType => messageType.IsAssignableFrom(interfaceType.GenericTypeArguments[0])));
}
