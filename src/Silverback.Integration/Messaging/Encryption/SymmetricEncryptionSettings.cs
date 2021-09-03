// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The encryption settings used to encrypt the messages.
    /// </summary>
    public class SymmetricEncryptionSettings : SymmetricEncryptionSettingsBase
    {
        /// <summary>
        ///     Gets or sets the key identifier to be sent in the header (see
        ///     <see cref="DefaultMessageHeaders.EncryptionKeyId" />). It will be used on the consumer side to
        ///     determine the correct key to be used to decrypt the message.
        /// </summary>
        public string? KeyIdentifier { get; set; }
    }
}
