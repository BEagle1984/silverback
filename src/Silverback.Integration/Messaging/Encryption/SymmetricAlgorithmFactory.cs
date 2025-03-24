// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Security.Cryptography;

namespace Silverback.Messaging.Encryption;

internal static class SymmetricAlgorithmFactory
{
    public static SymmetricAlgorithm CreateSymmetricAlgorithm(SymmetricEncryptionSettingsBase encryptionSettings, byte[] encryptionKey)
    {
        SymmetricAlgorithm algorithm = (SymmetricAlgorithm)CryptoConfig.CreateFromName(encryptionSettings.AlgorithmName)!;

        if (encryptionSettings.BlockSize != null)
            algorithm.BlockSize = encryptionSettings.BlockSize.Value;

        if (encryptionSettings.FeedbackSize != null)
            algorithm.FeedbackSize = encryptionSettings.FeedbackSize.Value;

        if (encryptionSettings.BlockSize != null)
            algorithm.BlockSize = encryptionSettings.BlockSize.Value;

        if (encryptionSettings.InitializationVector != null)
            algorithm.IV = encryptionSettings.InitializationVector;

        algorithm.Key = encryptionKey;

        if (encryptionSettings.CipherMode != null)
            algorithm.Mode = encryptionSettings.CipherMode.Value;

        if (encryptionSettings.PaddingMode != null)
            algorithm.Padding = encryptionSettings.PaddingMode.Value;

        return algorithm;
    }
}
