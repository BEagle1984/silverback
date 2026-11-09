// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class JsonMessageSerializerTests
{
    [Fact]
    public async Task SerializeAsync_ShouldSerialize()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = "{\"Content\":\"the message\"}"u8.ToArray();
        serialized.ReadAll().ShouldBe(expected);
    }

    [Fact]
    public async Task SerializeAsync_ShouldAddTypeHeader()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonMessageSerializer serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").ShouldBe(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_ShouldNotAddTypeHeader_WhenDisabled()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonMessageSerializer serializer = new(mustSetTypeHeader: false);

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").ShouldBeNull();
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnUnmodifiedStream()
    {
        MemoryStream messageStream = new("test"u8.ToArray());

        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageStream,
            [],
            TestProducerEndpoint.GetDefault());

        serialized.ShouldBeSameAs(messageStream);
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnUnmodifiedByteArray()
    {
        byte[] messageBytes = "test"u8.ToArray();

        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            [],
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().ShouldBe(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_ShouldSerialize_WhenUsingSourceGeneratedContext()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonMessageSerializer serializer = new(
            new JsonSerializerOptions
            {
                TypeInfoResolver = TestEventOneJsonSerializerContext.Default
            });

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = "{\"Content\":\"the message\"}"u8.ToArray();
        serialized.ReadAll().ShouldBe(expected);
        headers.GetValue("x-message-type").ShouldBe(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_ShouldSerialize_WhenCombiningSourceGeneratedContexts()
    {
        TestEventTwo message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonSerializerOptions options = new();
        options.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);
        options.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default);
        JsonMessageSerializer serializer = new(options);

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = "{\"Content\":\"the message\"}"u8.ToArray();
        serialized.ReadAll().ShouldBe(expected);
        headers.GetValue("x-message-type").ShouldBe(typeof(TestEventTwo).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_ShouldThrow_WhenTypeNotInSourceGeneratedContext()
    {
        TestEventTwo message = new() { Content = "the message" };

        JsonMessageSerializer serializer = new(
            new JsonSerializerOptions
            {
                TypeInfoResolver = TestEventOneJsonSerializerContext.Default
            });

        Func<Task> act = () => serializer.SerializeAsync(message, [], TestProducerEndpoint.GetDefault()).AsTask();

        Exception exception = await act.ShouldThrowAsync<NotSupportedException>();
        exception.Message.ShouldContain(nameof(TestEventTwo));
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnNull_WhenMessageIsNull()
    {
        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer
            .SerializeAsync(null, [], TestProducerEndpoint.GetDefault());

        serialized.ShouldBeNull();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        JsonMessageSerializer serializer1 = new();
        JsonMessageSerializer serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameSettings()
    {
        JsonMessageSerializer serializer1 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            });

        JsonMessageSerializer serializer2 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            });

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenBothHaveDefaultSettings()
    {
        JsonMessageSerializer serializer1 = new();
        JsonMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentSettings()
    {
        JsonMessageSerializer serializer1 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            });

        JsonMessageSerializer serializer2 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = false
            });

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeFalse();
    }
}
