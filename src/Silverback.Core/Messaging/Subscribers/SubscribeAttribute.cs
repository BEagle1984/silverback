// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     Used to identify the methods that have to be subscribed to the messages stream. The first parameter
///     of the subscriber method always correspond to the message and must be declared with a type
///     compatible with the message to be received (the message type, a base type or an implemented
///     interface) or a collection of items of that type. The methods can be either synchronous or
///     asynchronous (returning a <see cref="Task" />) and don't need to be publicly visible.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets a value indicating whether the method can be executed concurrently to other methods
    ///     handling the <b>same message</b>. The default value is <c>true</c> (the method will be executed
    ///     sequentially to other subscribers).
    /// </summary>
    public bool Exclusive { get; set; } = true;
}
