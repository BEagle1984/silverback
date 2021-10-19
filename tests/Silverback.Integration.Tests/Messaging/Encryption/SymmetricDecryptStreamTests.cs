// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    public class SymmetricDecryptStreamTests
    {
        private readonly byte[] _clearTextMessage = { 0x1, 0x2, 0x3, 0x4, 0x5 };

        [Fact]
        public async Task ReadAsync_UsingAesWithDefaultSettings_MessageIsSuccessfullyDecrypted()
        {
            var cypherMessage = new MemoryStream(
                new byte[]
                {
                    0xa5, 0x32, 0xe6, 0x46, 0xcd, 0xed, 0xc1, 0xeb, 0xc8, 0x0c, 0x74, 0xc0, 0x3e, 0x89, 0xe1, 0x8e,
                    0xb5, 0x6a, 0x50, 0x4a, 0xd7, 0x4f, 0x2c, 0x74, 0x1d, 0x0e, 0x63, 0x08, 0x4f, 0x31, 0xbe, 0x2c
                });

            var cryptoStream = new SymmetricDecryptStream(
                cypherMessage,
                new SymmetricDecryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await cryptoStream.ReadAllAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public void Read_UsingAesWithDefaultSettings_MessageIsSuccessfullyDecrypted()
        {
            var cypherMessage = new MemoryStream(
                new byte[]
                {
                    0xa5, 0x32, 0xe6, 0x46, 0xcd, 0xed, 0xc1, 0xeb, 0xc8, 0x0c, 0x74, 0xc0, 0x3e, 0x89, 0xe1, 0x8e,
                    0xb5, 0x6a, 0x50, 0x4a, 0xd7, 0x4f, 0x2c, 0x74, 0x1d, 0x0e, 0x63, 0x08, 0x4f, 0x31, 0xbe, 0x2c
                });

            var cryptoStream = new SymmetricDecryptStream(
                cypherMessage,
                new SymmetricDecryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = cryptoStream.ReadAll();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public async Task ReadAsync_UsingRijndaelWithCustomSettings_MessageIsSuccessfullyDecrypted()
        {
            var cypherMessage = new MemoryStream(
                new byte[]
                {
                    0xc5, 0x70, 0xbf, 0x56, 0x82, 0xc2, 0x5f, 0x71, 0x53, 0xa2, 0xa3, 0x12, 0xd0, 0x8b, 0x0d, 0x08, 0xa1,
                    0xde, 0x65, 0x32, 0xbe, 0x8c, 0xd0, 0xa6, 0x93, 0x75, 0x71, 0x81, 0xaa, 0x6d, 0xb5, 0x56
                });

            var cryptoStream = new SymmetricDecryptStream(
                cypherMessage,
                new SymmetricDecryptionSettings
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
            result.Should().BeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public void Read_UsingRijndaelWithCustomSettings_MessageIsSuccessfullyDecrypted()
        {
            var cypherMessage = new MemoryStream(
                new byte[]
                {
                    0xc5, 0x70, 0xbf, 0x56, 0x82, 0xc2, 0x5f, 0x71, 0x53, 0xa2, 0xa3, 0x12, 0xd0, 0x8b, 0x0d, 0x08, 0xa1,
                    0xde, 0x65, 0x32, 0xbe, 0x8c, 0xd0, 0xa6, 0x93, 0x75, 0x71, 0x81, 0xaa, 0x6d, 0xb5, 0x56
                });

            var cryptoStream = new SymmetricDecryptStream(
                cypherMessage,
                new SymmetricDecryptionSettings
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
            result.Should().BeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public async Task ReadAsync_SpecifyingIV_MessageIsSuccessfullyDecrypted()
        {
            var cypherMessage = new MemoryStream(
                new byte[]
                {
                    0x56, 0xd6, 0xcf, 0x6a, 0x76, 0xf1, 0xe6, 0x78, 0xb6, 0x34, 0x25, 0x81, 0x6f, 0x92, 0x48, 0xe2
                });

            var cryptoStream = new SymmetricDecryptStream(
                cypherMessage,
                new SymmetricDecryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });

            var result = await cryptoStream.ReadAllAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public void Read_SpecifyingIV_MessageIsSuccessfullyDecrypted()
        {
            var cypherMessage = new MemoryStream(
                new byte[]
                {
                    0x56, 0xd6, 0xcf, 0x6a, 0x76, 0xf1, 0xe6, 0x78, 0xb6, 0x34, 0x25, 0x81, 0x6f, 0x92, 0x48, 0xe2
                });

            var cryptoStream = new SymmetricDecryptStream(
                cypherMessage,
                new SymmetricDecryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });

            var result = cryptoStream.ReadAll();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public async Task ReadAsync_WithoutSpecifyingIV_IVIsExtractedAndMessageIsSuccessfullyDecrypted()
        {
            var cypherMessage = new MemoryStream(
                new byte[]
                {
                    0x6d, 0xda, 0xdb, 0x0c, 0xf2, 0xd9, 0xb3, 0x60, 0x77, 0xcf, 0x69, 0x09, 0x52, 0x24, 0x79, 0x0a, 0x82,
                    0x18, 0x01, 0x2f, 0xcb, 0x97, 0x2f, 0xeb, 0x4e, 0x4a, 0x6d, 0x8b, 0xad, 0x69, 0xf6, 0x84
                });

            var cryptoStream = new SymmetricDecryptStream(
                cypherMessage,
                new SymmetricDecryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await cryptoStream.ReadAllAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public void Read_WithoutSpecifyingIV_IVIsExtractedAndMessageIsSuccessfullyDecrypted()
        {
            var cypherMessage = new MemoryStream(
                new byte[]
                {
                    0x6d, 0xda, 0xdb, 0x0c, 0xf2, 0xd9, 0xb3, 0x60, 0x77, 0xcf, 0x69, 0x09, 0x52, 0x24, 0x79, 0x0a, 0x82,
                    0x18, 0x01, 0x2f, 0xcb, 0x97, 0x2f, 0xeb, 0x4e, 0x4a, 0x6d, 0x8b, 0xad, 0x69, 0xf6, 0x84
                });

            var cryptoStream = new SymmetricDecryptStream(
                cypherMessage,
                new SymmetricDecryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = cryptoStream.ReadAll();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public async Task ReadAsync_EmptyStream_EmptyStreamReturned()
        {
            var cryptoStream = new SymmetricDecryptStream(
                new MemoryStream(),
                new SymmetricDecryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await cryptoStream.ReadAllAsync();
            result.Should().BeEquivalentTo(Array.Empty<byte>());
        }

        [Fact]
        public void Read_EmptyStream_EmptyStreamReturned()
        {
            var cryptoStream = new SymmetricDecryptStream(
                new MemoryStream(),
                new SymmetricDecryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = cryptoStream.ReadAll();
            result.Should().BeEquivalentTo(Array.Empty<byte>());
        }

        private static byte[] GenerateKey(int size, int seed = 1) =>
            Enumerable.Range(seed, seed + (size / 8) - 1).Select(n => (byte)n).ToArray();
    }
}
