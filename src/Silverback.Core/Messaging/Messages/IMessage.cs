// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     This marker interface is used to tell Silverback that the type is actually a message
    ///     and enable features like automatic republishing. It is a good practice for all messages
    ///     to implement this interface but it's not mandatory.
    /// </summary>
    public interface IMessage
    {
    }
}