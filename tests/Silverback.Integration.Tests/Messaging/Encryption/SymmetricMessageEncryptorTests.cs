// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Encryption
{
    public class SymmetricMessageEncryptorTests
    {
        private const int AesDefaultBlockSizeInBytes = 16;

        private const int AesDefaultInitializationVectorSizeInBytes = 16;

        private readonly byte[] _clearTextMessage = { 0x1, 0x2, 0x3, 0x4, 0x5 };

        private readonly MessageHeaderCollection _headers = new MessageHeaderCollection();

        [Fact]
        public async Task Transform_UsingDefaultAesAlgorithmWithDefaultSettings_MessageIsSuccessfullyEncrypted()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await encryptor.TransformAsync(_clearTextMessage, _headers);

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
            result.Should().NotBeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public async Task Transform_UsingRijndaelWithCustomSettings_MessageIsSuccessfullyEncrypted()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    AlgorithmName = "Rijndael",
                    BlockSize = 128,
                    FeedbackSize = 64,
                    Key = GenerateKey(128),
                    CipherMode = CipherMode.ECB,
                    PaddingMode = PaddingMode.ISO10126
                });

            var result = await encryptor.TransformAsync(_clearTextMessage, _headers);

            result.Should().NotBeNull();
            result!.Length.Should().Be((128 * 2) / 8);
            result.Should().NotBeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public async Task Transform_SpecifyingIV_MessageIsSuccessfullyEncrypted()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });

            var result = await encryptor.TransformAsync(_clearTextMessage, _headers);

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes);
            result.Should().NotBeEquivalentTo(_clearTextMessage);
        }

        [Fact]
        public async Task Transform_SpecifyingIV_IVIsNotPrepended()
        {
            var iv = GenerateKey(128);

            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = iv
                });

            var result = await encryptor.TransformAsync(_clearTextMessage, _headers);

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes);
            result.Take(iv.Length).Should().NotBeEquivalentTo(iv);
        }

        [Fact]
        public async Task Transform_WithoutSpecifyingIV_IVIsGeneratedAndPrepended()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await encryptor.TransformAsync(_clearTextMessage, _headers);

            result.Should().NotBeNull();
            result!.Length.Should().Be(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
        }

        [Fact]
        public async Task Transform_TwiceWithSameIV_ResultIsEqual()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256),
                    InitializationVector = GenerateKey(128)
                });

            var result1 = await encryptor.TransformAsync(_clearTextMessage, _headers);
            var result2 = await encryptor.TransformAsync(_clearTextMessage, _headers);

            result2.Should().BeEquivalentTo(result1);
        }

        [Fact]
        public async Task Transform_TwiceWithoutSpecifyingIV_ResultIsDifferent()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result1 = await encryptor.TransformAsync(_clearTextMessage, _headers);
            var result2 = await encryptor.TransformAsync(_clearTextMessage, _headers);

            result2.Should().NotBeEquivalentTo(result1);
        }

        [Fact]
        public async Task Transform_TwiceWithoutSpecifyingIV_GeneratedIVIsDifferent()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var iv1 = (await encryptor.TransformAsync(_clearTextMessage, _headers))
                .Take(AesDefaultInitializationVectorSizeInBytes);
            var iv2 = (await encryptor.TransformAsync(_clearTextMessage, _headers))
                .Take(AesDefaultInitializationVectorSizeInBytes);

            iv2.Should().NotBeEquivalentTo(iv1);
        }

        [Fact]
        public async Task Transform_Null_NullIsReturned()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await encryptor.TransformAsync(null, _headers);

            result.Should().BeNull();
        }

        [Fact]
        public async Task Transform_EmptyArray_EmptyArrayIsReturned()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await encryptor.TransformAsync(Array.Empty<byte>(), _headers);

            result.Should().BeEmpty();
        }

        private static byte[] GenerateKey(int size, int seed = 1) =>
            Enumerable.Range(seed, (seed + (size / 8)) - 1).Select(n => (byte)n).ToArray();
    }
}
