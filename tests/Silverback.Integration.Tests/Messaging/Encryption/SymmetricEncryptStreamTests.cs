// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Encryption;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Encryption;

public class SymmetricEncryptStreamTests
{
    private const int AesDefaultBlockSizeInBytes = 16;

    private const int AesDefaultInitializationVectorSizeInBytes = 16;

    private static readonly byte[] ClearTextMessage = [0x1, 0x2, 0x3, 0x4, 0x5];

    [Fact]
    public async Task ReadAsync_UsingDefaultAesAlgorithmWithDefaultSettings_MessageIsSuccessfullyEncrypted()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = await cryptoStream.ReadAllAsync();

        result.Should().NotBeNull();
        result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
        result.Should().NotBeEquivalentTo(ClearTextMessage);
    }

    [Fact]
    public void Read_UsingDefaultAesAlgorithmWithDefaultSettings_MessageIsSuccessfullyEncrypted()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = cryptoStream.ReadAll();

        result.Should().NotBeNull();
        result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
        result.Should().NotBeEquivalentTo(ClearTextMessage);
    }

    [Fact]
    public async Task ReadAsync_UsingRijndaelWithCustomSettings_MessageIsSuccessfullyEncrypted()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                AlgorithmName = "Rijndael",
                BlockSize = 128,
                FeedbackSize = 64,
                Key = GenerateKey(128),
                CipherMode = CipherMode.ECB,
                PaddingMode = PaddingMode.ISO10126
            });

        byte[]? result = await cryptoStream.ReadAllAsync();
        result.Should().NotBeNull();
        result!.Length.Should().Be(128 * 2 / 8);
        result.Should().NotBeEquivalentTo(ClearTextMessage);
    }

    [Fact]
    public void Read_UsingRijndaelWithCustomSettings_MessageIsSuccessfullyEncrypted()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                AlgorithmName = "Rijndael",
                BlockSize = 128,
                FeedbackSize = 64,
                Key = GenerateKey(128),
                CipherMode = CipherMode.ECB,
                PaddingMode = PaddingMode.ISO10126
            });

        byte[]? result = cryptoStream.ReadAll();
        result.Should().NotBeNull();
        result!.Length.Should().Be(128 * 2 / 8);
        result.Should().NotBeEquivalentTo(ClearTextMessage);
    }

    [Fact]
    public async Task ReadAsync_SpecifyingIV_MessageIsSuccessfullyEncrypted()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = GenerateKey(128)
            });

        byte[]? result = await cryptoStream.ReadAllAsync();

        result.Should().NotBeNull();
        result!.Length.Should().Be(AesDefaultBlockSizeInBytes);
        result.Should().NotBeEquivalentTo(ClearTextMessage);
    }

    [Fact]
    public void Read_SpecifyingIV_MessageIsSuccessfullyEncrypted()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = GenerateKey(128)
            });

        byte[]? result = cryptoStream.ReadAll();

        result.Should().NotBeNull();
        result!.Length.Should().Be(AesDefaultBlockSizeInBytes);
        result.Should().NotBeEquivalentTo(ClearTextMessage);
    }

    [Fact]
    public async Task ReadAsync_SpecifyingIV_IVIsNotPrepended()
    {
        byte[] iv = GenerateKey(128);

        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = iv
            });

        byte[]? result = await cryptoStream.ReadAllAsync();

        result.Should().NotBeNull();
        result!.Length.Should().Be(AesDefaultBlockSizeInBytes);

        result.Take(iv.Length).Should().NotBeEquivalentTo(iv);
    }

    [Fact]
    public void Read_SpecifyingIV_IVIsNotPrepended()
    {
        byte[] iv = GenerateKey(128);

        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = iv
            });

        byte[]? result = cryptoStream.ReadAll();

        result.Should().NotBeNull();
        result!.Length.Should().Be(AesDefaultBlockSizeInBytes);

        result.Take(iv.Length).Should().NotBeEquivalentTo(iv);
    }

    [Fact]
    public async Task ReadAsync_WithoutSpecifyingIV_IVIsGeneratedAndPrepended()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = await cryptoStream.ReadAllAsync();
        result.Should().NotBeNull();
        result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
    }

    [Fact]
    public void Read_WithoutSpecifyingIV_IVIsGeneratedAndPrepended()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = cryptoStream.ReadAll();
        result.Should().NotBeNull();
        result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
    }

    [Fact]
    public async Task ReadAsync_TwiceWithSameIV_ResultIsEqual()
    {
        SymmetricEncryptStream cryptoStream1 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = GenerateKey(128)
            });
        SymmetricEncryptStream cryptoStream2 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = GenerateKey(128)
            });

        byte[]? result1 = await cryptoStream1.ReadAllAsync();
        byte[]? result2 = await cryptoStream2.ReadAllAsync();
        result2.Should().BeEquivalentTo(result1);
    }

    [Fact]
    public void Read_TwiceWithSameIV_ResultIsEqual()
    {
        SymmetricEncryptStream cryptoStream1 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = GenerateKey(128)
            });
        SymmetricEncryptStream cryptoStream2 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = GenerateKey(128)
            });

        byte[]? result1 = cryptoStream1.ReadAll();
        byte[]? result2 = cryptoStream2.ReadAll();
        result2.Should().BeEquivalentTo(result1);
    }

    [Fact]
    public async Task ReadAsync_TwiceWithoutSpecifyingIV_ResultIsDifferent()
    {
        SymmetricEncryptStream cryptoStream1 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });
        SymmetricEncryptStream cryptoStream2 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result1 = await cryptoStream1.ReadAllAsync();
        byte[]? result2 = await cryptoStream2.ReadAllAsync();
        result2.Should().NotBeEquivalentTo(result1);
    }

    [Fact]
    public void Read_TwiceWithoutSpecifyingIV_ResultIsDifferent()
    {
        SymmetricEncryptStream cryptoStream1 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });
        SymmetricEncryptStream cryptoStream2 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result1 = cryptoStream1.ReadAll();
        byte[]? result2 = cryptoStream2.ReadAll();
        result2.Should().NotBeEquivalentTo(result1);
    }

    [Fact]
    public async Task ReadAsync_TwiceWithoutSpecifyingIV_GeneratedIVIsDifferent()
    {
        SymmetricEncryptStream cryptoStream1 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });
        SymmetricEncryptStream cryptoStream2 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result1 = await cryptoStream1.ReadAllAsync();
        byte[]? result2 = await cryptoStream2.ReadAllAsync();

        IEnumerable<byte> iv1 = result1!.Take(AesDefaultInitializationVectorSizeInBytes);
        IEnumerable<byte> iv2 = result2!.Take(AesDefaultInitializationVectorSizeInBytes);

        iv2.Should().NotBeEquivalentTo(iv1);
    }

    [Fact]
    public void Read_TwiceWithoutSpecifyingIV_GeneratedIVIsDifferent()
    {
        SymmetricEncryptStream cryptoStream1 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });
        SymmetricEncryptStream cryptoStream2 = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result1 = cryptoStream1.ReadAll();
        byte[]? result2 = cryptoStream2.ReadAll();

        IEnumerable<byte> iv1 = result1!.Take(AesDefaultInitializationVectorSizeInBytes);
        IEnumerable<byte> iv2 = result2!.Take(AesDefaultInitializationVectorSizeInBytes);

        iv2.Should().NotBeEquivalentTo(iv1);
    }

    [Fact]
    public async Task ReadAsync_EmptyStream_EmptyStreamEncrypted()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = await cryptoStream.ReadAllAsync();

        result.Should().HaveCount(32);
    }

    [Fact]
    public void Read_EmptyStream_EmptyStreamEncrypted()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = cryptoStream.ReadAll();

        result.Should().HaveCount(32);
    }

    private static byte[] GenerateKey(int size, int seed = 1) =>
        Enumerable.Range(seed, seed + (size / 8) - 1).Select(n => (byte)n).ToArray();
}
