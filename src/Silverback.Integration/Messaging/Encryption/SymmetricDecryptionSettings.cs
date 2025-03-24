// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Encryption;

/// <summary>
///     The decryption settings used to decrypt the messages.
/// </summary>
public record SymmetricDecryptionSettings : SymmetricEncryptionSettingsBase, IDecryptionSettings
{
    /// <summary>
    ///     Gets the function to be used to retrieve the decryption key.
    /// </summary>
    public Func<string?, byte[]>? KeyProvider { get; init; }

    /// <inheritdoc cref="SymmetricEncryptionSettingsBase.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (KeyProvider == null && Key == null)
            throw new BrokerConfigurationException($"A {nameof(Key)} or a {nameof(KeyProvider)} is required.");

        if (KeyProvider != null && Key != null)
            throw new BrokerConfigurationException($"Cannot set both the {nameof(Key)} and the {nameof(KeyProvider)}.");
    }
}
