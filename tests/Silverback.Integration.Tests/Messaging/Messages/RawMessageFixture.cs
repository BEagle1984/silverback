// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class RawMessageFixture
{
    [Fact]
    public void ImplicitOperator_ShouldConvertToStream()
    {
        RawMessage message1 = new(new MemoryStream([0x01, 0x02, 0x03]));
        RawMessage<TestEventOne> message2 = new(new MemoryStream([0x01, 0x02, 0x03]));

        Stream? content1 = message1;
        Stream? content2 = message2;

        content1.ShouldBe(message1.Content);
        content2.ShouldBe(message2.Content);
    }

    [Fact]
    public void ImplicitOperator_ShouldConvertToByteArray()
    {
        RawMessage message1 = new(new MemoryStream([0x01, 0x02, 0x03]));
        RawMessage<TestEventOne> message2 = new(new MemoryStream([0x01, 0x02, 0x03]));

        byte[]? content1 = message1;
        byte[]? content2 = message2;

        content1.ShouldBe([0x01, 0x02, 0x03]);
        content2.ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public void ImplicitOperator_ShouldConvertFromStream()
    {
        Stream content1 = new MemoryStream([0x01, 0x02, 0x03]);
        Stream content2 = new MemoryStream([0x01, 0x02, 0x03]);

        RawMessage message1 = content1;
        RawMessage<TestEventOne> message2 = content2;

        message1.Content.ShouldBe(content1);
        message2.Content.ShouldBe(content2);
    }

    [Fact]
    public void ImplicitOperator_ShouldConvertFromByteArray()
    {
        byte[] content1 = [0x01, 0x02, 0x03];
        byte[] content2 = [0x01, 0x02, 0x03];

        RawMessage message1 = content1;
        RawMessage<TestEventOne> message2 = content2;

        message1.Content.ReadAll().ShouldBe(content1);
        message2.Content.ReadAll().ShouldBe(content2);
    }

    [Fact]
    public void FromStream_ShouldCreateMessage()
    {
        Stream content1 = new MemoryStream([0x01, 0x02, 0x03]);
        Stream content2 = new MemoryStream([0x01, 0x02, 0x03]);

        RawMessage message1 = RawMessage.FromStream(content1);
        RawMessage<TestEventOne> message2 = RawMessage<TestEventOne>.FromStream(content2);

        message1.Content.ShouldBe(content1);
        message2.Content.ShouldBe(content2);
    }

    [Fact]
    public void FromByteArray_ShouldCreateMessage()
    {
        byte[] content1 = [0x01, 0x02, 0x03];
        byte[] content2 = [0x01, 0x02, 0x03];

        RawMessage message1 = RawMessage.FromByteArray(content1);
        RawMessage<TestEventOne> message2 = RawMessage<TestEventOne>.FromByteArray(content2);

        message1.Content.ReadAll().ShouldBe(content1);
        message2.Content.ReadAll().ShouldBe(content2);
    }

    [Fact]
    public void ToStream_ShouldReturnContent()
    {
        RawMessage message1 = new(new MemoryStream([0x01, 0x02, 0x03]));
        RawMessage<TestEventOne> message2 = new(new MemoryStream([0x01, 0x02, 0x03]));

        Stream? content1 = message1.ToStream();
        Stream? content2 = message2.ToStream();

        content1.ReadAll().ShouldBe([0x01, 0x02, 0x03]);
        content2.ReadAll().ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public void ToByteArray_ShouldReturnContent()
    {
        RawMessage message1 = new(new MemoryStream([0x01, 0x02, 0x03]));
        RawMessage<TestEventOne> message2 = new(new MemoryStream([0x01, 0x02, 0x03]));

        byte[]? content1 = message1.ToByteArray();
        byte[]? content2 = message2.ToByteArray();

        content1.ShouldBe([0x01, 0x02, 0x03]);
        content2.ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public async Task ToByteArrayAsync_ShouldReturnContent()
    {
        RawMessage message1 = new(new MemoryStream([0x01, 0x02, 0x03]));
        RawMessage<TestEventOne> message2 = new(new MemoryStream([0x01, 0x02, 0x03]));

        byte[]? content1 = await message1.ToByteArrayAsync();
        byte[]? content2 = await message2.ToByteArrayAsync();

        content1.ShouldBe([0x01, 0x02, 0x03]);
        content2.ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public void ToStream_ShouldReturnNull_WhenContentIsNull()
    {
        RawMessage message1 = new(null);
        RawMessage<TestEventOne> message2 = new(null);

        Stream? content1 = message1.ToStream();
        Stream? content2 = message2.ToStream();

        content1.ShouldBeNull();
        content2.ShouldBeNull();
    }

    [Fact]
    public void ToByteArray_ShouldReturnNull_WhenContentIsNull()
    {
        RawMessage message1 = new(null);
        RawMessage<TestEventOne> message2 = new(null);

        byte[]? content1 = message1.ToByteArray();
        byte[]? content2 = message2.ToByteArray();

        content1.ShouldBeNull();
        content2.ShouldBeNull();
    }

    [Fact]
    public async Task ToByteArrayAsync_ShouldReturnNull_WhenContentIsNull()
    {
        RawMessage message1 = new(null);
        RawMessage<TestEventOne> message2 = new(null);

        byte[]? content1 = await message1.ToByteArrayAsync();
        byte[]? content2 = await message2.ToByteArrayAsync();

        content1.ShouldBeNull();
        content2.ShouldBeNull();
    }
}
