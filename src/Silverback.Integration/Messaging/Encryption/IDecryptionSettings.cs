// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;

namespace Silverback.Messaging.Encryption;

/// <summary>
///     The interface to be implemented by the decryption settings classes such as <see cref="SymmetricDecryptionSettings" />.
/// </summary>
public interface IDecryptionSettings : IValidatableSettings;
