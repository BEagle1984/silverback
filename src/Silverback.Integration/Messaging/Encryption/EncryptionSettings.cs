// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The base class for <see cref="SymmetricEncryptionSettings" /> and other future encryption types.
    /// </summary>
    public abstract class EncryptionSettings : IValidatableEndpointSettings
    {
        /// <inheritdoc />
        public abstract void Validate();
    }
}
