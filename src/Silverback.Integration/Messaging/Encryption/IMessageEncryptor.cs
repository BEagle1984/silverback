// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     Encrypts the messages being produced.
    /// </summary>
    public interface IMessageEncryptor : IRawMessageTransformer
    {
    }
}
