// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;

namespace Silverback.Messaging.Encryption;

/// <summary>
///     The interface to be implemented by the encryption settings classes such as <see cref="SymmetricEncryptionSettings" />.
/// </summary>
public interface IEncryptionSettings : IValidatableSettings;
