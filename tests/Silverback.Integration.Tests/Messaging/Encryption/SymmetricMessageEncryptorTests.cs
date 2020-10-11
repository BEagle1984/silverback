// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Encryption
{
    public class SymmetricMessageEncryptorTests
    {
        // TODO: Fix encryption

        private const int AesDefaultBlockSizeInBytes = 16;

        private const int AesDefaultInitializationVectorSizeInBytes = 16;

        private readonly MemoryStream _clearTextMessage = new MemoryStream(new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5 });

        private readonly MessageHeaderCollection _headers = new MessageHeaderCollection();

        [Fact(Skip = "To be fixed")]
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

        [Fact(Skip = "To be fixed")]
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

        [Fact(Skip = "To be fixed")]
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

        [Fact(Skip = "To be fixed")]
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

            (await result.ReadAsync(iv.Length)).Should().NotBeEquivalentTo(iv);
        }

        [Fact(Skip = "To be fixed")]
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

        [Fact(Skip = "To be fixed")]
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

        [Fact(Skip = "To be fixed")]
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

        [Fact(Skip = "To be fixed")]
        public async Task Transform_TwiceWithoutSpecifyingIV_GeneratedIVIsDifferent()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var encrypted1 = await encryptor.TransformAsync(_clearTextMessage, _headers);
            var iv1 = encrypted1.ReadAsync(AesDefaultInitializationVectorSizeInBytes);

            var encrypted2 = await encryptor.TransformAsync(_clearTextMessage, _headers);
            var iv2 = encrypted2.ReadAsync(AesDefaultInitializationVectorSizeInBytes);

            iv2.Should().NotBeEquivalentTo(iv1);
        }

        [Fact(Skip = "To be fixed")]
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

        [Fact(Skip = "To be fixed")]
        public async Task Transform_EmptyStream_EmptyStreamIsReturned()
        {
            var encryptor = new SymmetricMessageEncryptor(
                new SymmetricEncryptionSettings
                {
                    Key = GenerateKey(256)
                });

            var result = await encryptor.TransformAsync(new MemoryStream(), _headers);

            result.Should().BeEquivalentTo(new MemoryStream());
        }

        private static byte[] GenerateKey(int size, int seed = 1) =>
            Enumerable.Range(seed, (seed + (size / 8)) - 1).Select(n => (byte)n).ToArray();
    }
}
