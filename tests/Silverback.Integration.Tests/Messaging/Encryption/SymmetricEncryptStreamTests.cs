// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Encryption;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Encryption
{
    public class SymmetricEncryptStreamTests
    {
        private const int AesDefaultBlockSizeInBytes = 16;

        private const int AesDefaultInitializationVectorSizeInBytes = 16;

        private static readonly byte[] ClearTextMessage = { 0x1, 0x2, 0x3, 0x4, 0x5 };

        [Fact]
        public async Task ReadAsync_UsingDefaultAesAlgorithmWithDefaultSettings_MessageIsSuccessfullyEncrypted()
        {
            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await cryptoStream.ReadAllAsync();

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
            result.Should().NotBeEquivalentTo(ClearTextMessage);
        }

        [Fact]
        public void Read_UsingDefaultAesAlgorithmWithDefaultSettings_MessageIsSuccessfullyEncrypted()
        {
            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = cryptoStream.ReadAll();

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
            result.Should().NotBeEquivalentTo(ClearTextMessage);
        }

        [Fact]
        public async Task ReadAsync_UsingRijndaelWithCustomSettings_MessageIsSuccessfullyEncrypted()
        {
            var cryptoStream = new SymmetricEncryptStream(
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

            var result = await cryptoStream.ReadAllAsync();
            result.Should().NotBeNull();
            result!.Length.Should().Be(128 * 2 / 8);
            result.Should().NotBeEquivalentTo(ClearTextMessage);
        }

        [Fact]
        public void Read_UsingRijndaelWithCustomSettings_MessageIsSuccessfullyEncrypted()
        {
            var cryptoStream = new SymmetricEncryptStream(
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

            var result = cryptoStream.ReadAll();
            result.Should().NotBeNull();
            result!.Length.Should().Be(128 * 2 / 8);
            result.Should().NotBeEquivalentTo(ClearTextMessage);
        }

        [Fact]
        public async Task ReadAsync_SpecifyingIV_MessageIsSuccessfullyEncrypted()
        {
            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });

            var result = await cryptoStream.ReadAllAsync();

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes);
            result.Should().NotBeEquivalentTo(ClearTextMessage);
        }

        [Fact]
        public void Read_SpecifyingIV_MessageIsSuccessfullyEncrypted()
        {
            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });

            var result = cryptoStream.ReadAll();

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes);
            result.Should().NotBeEquivalentTo(ClearTextMessage);
        }

        [Fact]
        public async Task ReadAsync_SpecifyingIV_IVIsNotPrepended()
        {
            var iv = GenerateKey(128);

            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = iv
                });

            var result = await cryptoStream.ReadAllAsync();

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes);

            result.Take(iv.Length).Should().NotBeEquivalentTo(iv);
        }

        [Fact]
        public void Read_SpecifyingIV_IVIsNotPrepended()
        {
            var iv = GenerateKey(128);

            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = iv
                });

            var result = cryptoStream.ReadAll();

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes);

            result.Take(iv.Length).Should().NotBeEquivalentTo(iv);
        }

        [Fact]
        public async Task ReadAsync_WithoutSpecifyingIV_IVIsGeneratedAndPrepended()
        {
            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await cryptoStream.ReadAllAsync();
            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
        }

        [Fact]
        public void Read_WithoutSpecifyingIV_IVIsGeneratedAndPrepended()
        {
            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = cryptoStream.ReadAll();
            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
        }

        [Fact]
        public async Task ReadAsync_TwiceWithSameIV_ResultIsEqual()
        {
            var cryptoStream1 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });
            var cryptoStream2 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });

            var result1 = await cryptoStream1.ReadAllAsync();
            var result2 = await cryptoStream2.ReadAllAsync();
            result2.Should().BeEquivalentTo(result1);
        }

        [Fact]
        public void Read_TwiceWithSameIV_ResultIsEqual()
        {
            var cryptoStream1 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });
            var cryptoStream2 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });

            var result1 = cryptoStream1.ReadAll();
            var result2 = cryptoStream2.ReadAll();
            result2.Should().BeEquivalentTo(result1);
        }

        [Fact]
        public async Task ReadAsync_TwiceWithoutSpecifyingIV_ResultIsDifferent()
        {
            var cryptoStream1 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });
            var cryptoStream2 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result1 = await cryptoStream1.ReadAllAsync();
            var result2 = await cryptoStream2.ReadAllAsync();
            result2.Should().NotBeEquivalentTo(result1);
        }

        [Fact]
        public void Read_TwiceWithoutSpecifyingIV_ResultIsDifferent()
        {
            var cryptoStream1 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });
            var cryptoStream2 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result1 = cryptoStream1.ReadAll();
            var result2 = cryptoStream2.ReadAll();
            result2.Should().NotBeEquivalentTo(result1);
        }

        [Fact]
        public async Task ReadAsync_TwiceWithoutSpecifyingIV_GeneratedIVIsDifferent()
        {
            var cryptoStream1 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });
            var cryptoStream2 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result1 = await cryptoStream1.ReadAllAsync();
            var result2 = await cryptoStream2.ReadAllAsync();

            var iv1 = result1!.Take(AesDefaultInitializationVectorSizeInBytes);
            var iv2 = result2!.Take(AesDefaultInitializationVectorSizeInBytes);

            iv2.Should().NotBeEquivalentTo(iv1);
        }

        [Fact]
        public void Read_TwiceWithoutSpecifyingIV_GeneratedIVIsDifferent()
        {
            var cryptoStream1 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });
            var cryptoStream2 = new SymmetricEncryptStream(
                new MemoryStream(ClearTextMessage),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result1 = cryptoStream1.ReadAll();
            var result2 = cryptoStream2.ReadAll();

            var iv1 = result1!.Take(AesDefaultInitializationVectorSizeInBytes);
            var iv2 = result2!.Take(AesDefaultInitializationVectorSizeInBytes);

            iv2.Should().NotBeEquivalentTo(iv1);
        }

        [Fact]
        public async Task ReadAsync_EmptyStream_EmptyStreamEncrypted()
        {
            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await cryptoStream.ReadAllAsync();

            result.Should().HaveCount(32);
        }

        [Fact]
        public void Read_EmptyStream_EmptyStreamEncrypted()
        {
            var cryptoStream = new SymmetricEncryptStream(
                new MemoryStream(),
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = cryptoStream.ReadAll();

            result.Should().HaveCount(32);
        }

        private static byte[] GenerateKey(int size, int seed = 1) =>
            Enumerable.Range(seed, seed + (size / 8) - 1).Select(n => (byte)n).ToArray();
    }
}
