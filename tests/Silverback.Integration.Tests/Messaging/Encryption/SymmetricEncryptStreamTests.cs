// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Encryption;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Encryption;

public class SymmetricEncryptStreamTests
{
    private const int WindowOffset = 10;

    private const int WindowPadding = 20;

    private const byte Filler = 0xAA;

    private const int AesDefaultBlockSizeInBytes = 16;

    private const int AesDefaultInitializationVectorSizeInBytes = 16;

    private static readonly byte[] ClearTextMessage = [0x1, 0x2, 0x3, 0x4, 0x5];

    [Fact]
    public async Task ReadAsync_ShouldEncrypt_WhenUsingDefaultAesAlgorithmWithDefaultSettings()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = await cryptoStream.ReadAllAsync();

        result.ShouldNotBeNull();
        result.Length.ShouldBe(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
        result.ShouldNotBe(ClearTextMessage);
    }

    [Fact]
    public void Read_ShouldEncrypt_WhenUsingDefaultAesAlgorithmWithDefaultSettings()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = cryptoStream.ReadAll();

        result.ShouldNotBeNull();
        result.Length.ShouldBe(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
        result.ShouldNotBe(ClearTextMessage);
    }

    [Fact]
    public async Task ReadAsync_ShouldEncrypt_WhenUsingRijndaelWithCustomSettings()
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
        result.ShouldNotBeNull();
        result.Length.ShouldBe(128 * 2 / 8);
        result.ShouldNotBe(ClearTextMessage);
    }

    [Fact]
    public void Read_ShouldEncrypt_WhenUsingRijndaelWithCustomSettings()
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
        result.ShouldNotBeNull();
        result.Length.ShouldBe(128 * 2 / 8);
        result.ShouldNotBe(ClearTextMessage);
    }

    [Fact]
    public async Task ReadAsync_ShouldEncrypt_WhenSpecifyingIV()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = GenerateKey(128)
            });

        byte[]? result = await cryptoStream.ReadAllAsync();

        result.ShouldNotBeNull();
        result.Length.ShouldBe(AesDefaultBlockSizeInBytes);
        result.ShouldNotBe(ClearTextMessage);
    }

    [Fact]
    public void Read_ShouldEncrypt_WhenSpecifyingIV()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256),
                InitializationVector = GenerateKey(128)
            });

        byte[]? result = cryptoStream.ReadAll();

        result.ShouldNotBeNull();
        result.Length.ShouldBe(AesDefaultBlockSizeInBytes);
        result.ShouldNotBe(ClearTextMessage);
    }

    [Fact]
    public async Task ReadAsync_ShouldNotPrependIV_WhenHardcodedIV()
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

        result.ShouldNotBeNull();
        result.Length.ShouldBe(AesDefaultBlockSizeInBytes);

        result.Take(iv.Length).ShouldNotBe(iv);
    }

    [Fact]
    public void Read_ShouldNotPrependIV_WhenHardcodedIV()
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

        result.ShouldNotBeNull();
        result.Length.ShouldBe(AesDefaultBlockSizeInBytes);

        result.Take(iv.Length).ShouldNotBe(iv);
    }

    [Fact]
    public async Task ReadAsync_ShouldGenerateAndPrependIV_WhenIVNotSet()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = await cryptoStream.ReadAllAsync();
        result.ShouldNotBeNull();
        result.Length.ShouldBe(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
    }

    [Fact]
    public void Read_ShouldGenerateAndPrependIV_WhenIVNotSet()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(ClearTextMessage),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = cryptoStream.ReadAll();
        result.ShouldNotBeNull();
        result.Length.ShouldBe(AesDefaultBlockSizeInBytes + AesDefaultInitializationVectorSizeInBytes);
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnSameStream_WhenUsingSameIV()
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
        result2.ShouldBe(result1);
    }

    [Fact]
    public void Read_ShouldReturnSameStream_WhenUsingSameIV()
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
        result2.ShouldBe(result1);
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnDifferentStream_WhenIVNotSet()
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
        result2.ShouldNotBe(result1);
    }

    [Fact]
    public void Read_ShouldReturnDifferentStream_WhenIVNotSet()
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
        result2.ShouldNotBe(result1);
    }

    [Fact]
    public async Task ReadAsync_ShouldGenerateDifferentIV_WhenIVNotSet()
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

        iv2.ShouldNotBe(iv1);
    }

    [Fact]
    public void Read_ShouldGenerateDifferentIV_WhenIVNotSet()
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

        iv2.ShouldNotBe(iv1);
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnEmptyStream_WhenEmptyStream()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = await cryptoStream.ReadAllAsync();

        result.ShouldNotBeNull();
        result.Length.ShouldBe(32);
    }

    [Fact]
    public void Read_ShouldReturnEmptyStream_WhenEmptyStream()
    {
        SymmetricEncryptStream cryptoStream = new(
            new MemoryStream(),
            new SymmetricEncryptionSettings
            {
                Key = GenerateKey(256)
            });

        byte[]? result = cryptoStream.ReadAll();

        result.ShouldNotBeNull();
        result.Length.ShouldBe(32);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(32)]
    public void Read_ShouldRespectOffsetAndCount_WhenReadingInSmallSteps(int count)
    {
        byte[] key = GenerateKey(256);
        SymmetricEncryptStream cryptoStream = new(new MemoryStream(ClearTextMessage), new SymmetricEncryptionSettings { Key = key });
        List<byte> result = [];
        byte[] buffer = new byte[WindowOffset + count + WindowPadding];

        while (true)
        {
            Array.Fill(buffer, Filler);

            int read = cryptoStream.Read(buffer, WindowOffset, count);
            if (read == 0)
                break;

            read.ShouldBeLessThanOrEqualTo(count);
            AssertUntouched(buffer, WindowOffset, count);
            result.AddRange(buffer.AsSpan(WindowOffset, read));
        }

        Decrypt([.. result], key).ShouldBe(ClearTextMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(32)]
    [SuppressMessage("Performance", "CA1835:Prefer the memory-based overloads of ReadAsync", Justification = "The byte[] overload is the one under test")]
    public async Task ReadAsync_ShouldRespectOffsetAndCount_WhenReadingInSmallSteps(int count)
    {
        byte[] key = GenerateKey(256);
        SymmetricEncryptStream cryptoStream = new(new MemoryStream(ClearTextMessage), new SymmetricEncryptionSettings { Key = key });
        List<byte> result = [];
        byte[] buffer = new byte[WindowOffset + count + WindowPadding];

        while (true)
        {
            Array.Fill(buffer, Filler);

            int read = await cryptoStream.ReadAsync(buffer, WindowOffset, count, CancellationToken.None);
            if (read == 0)
                break;

            read.ShouldBeLessThanOrEqualTo(count);
            AssertUntouched(buffer, WindowOffset, count);
            result.AddRange(buffer.AsSpan(WindowOffset, read));
        }

        Decrypt([.. result], key).ShouldBe(ClearTextMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(32)]
    public void Read_ShouldRespectSpanWindowAndIncludePrefix_WhenReadingSpanInSmallSteps(int count)
    {
        byte[] key = GenerateKey(256);
        SymmetricEncryptStream cryptoStream = new(new MemoryStream(ClearTextMessage), new SymmetricEncryptionSettings { Key = key });
        List<byte> result = [];
        byte[] buffer = new byte[WindowOffset + count + WindowPadding];

        while (true)
        {
            Array.Fill(buffer, Filler);

            int read = cryptoStream.Read(buffer.AsSpan(WindowOffset, count));
            if (read == 0)
                break;

            read.ShouldBeLessThanOrEqualTo(count);
            AssertUntouched(buffer, WindowOffset, count);
            result.AddRange(buffer.AsSpan(WindowOffset, read));
        }

        Decrypt([.. result], key).ShouldBe(ClearTextMessage);
    }

    [Fact]
    public void Read_ShouldThrowArgumentNullException_WhenBufferIsNull()
    {
        byte[] key = GenerateKey(256);
        SymmetricEncryptStream cryptoStream = new(new MemoryStream(ClearTextMessage), new SymmetricEncryptionSettings { Key = key });

        Action act = () => _ = cryptoStream.Read(null!, 0, 1);

        act.ShouldThrow<ArgumentNullException>();
    }

    private static void AssertUntouched(byte[] buffer, int offset, int count)
    {
        buffer.Take(offset).ShouldAllBe(value => value == Filler);
        buffer.Skip(offset + count).ShouldAllBe(value => value == Filler);
    }

    private static byte[] Decrypt(byte[] encrypted, byte[] key)
    {
        using SymmetricDecryptStream decryptStream = new(new MemoryStream(encrypted), new SymmetricDecryptionSettings { Key = key });
        return decryptStream.ReadAll()!;
    }

    private static byte[] GenerateKey(int size, int seed = 1) =>
        [.. Enumerable.Range(seed, seed + (size / 8) - 1).Select(n => (byte)n)];
}
