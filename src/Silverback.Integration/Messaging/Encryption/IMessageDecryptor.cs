// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     Decrypts the messages being consumed.
    /// </summary>
    public interface IMessageDecryptor : IRawMessageTransformer
    {
    }
}