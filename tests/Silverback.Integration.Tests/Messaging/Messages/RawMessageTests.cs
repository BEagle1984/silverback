// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class RawMessageTests
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
    public void FromByteArray_ShouldNotShareByteArray()
    {
        byte[] content1 = [0x01, 0x02, 0x03];
        byte[] content2 = [0x01, 0x02, 0x03];
        byte[] content3 = [0x01, 0x02, 0x03];
        byte[] content4 = [0x01, 0x02, 0x03];

        RawMessage message1 = RawMessage.FromByteArray(content1);
        RawMessage<TestEventOne> message2 = RawMessage<TestEventOne>.FromByteArray(content2);
        RawMessage message3 = content3;
        RawMessage<TestEventOne> message4 = content4;

        message1.Content.ReadAll().ShouldNotBeSameAs(content1);
        message2.Content.ReadAll().ShouldNotBeSameAs(content2);
        message3.Content.ReadAll().ShouldNotBeSameAs(content3);
        message4.Content.ReadAll().ShouldNotBeSameAs(content4);
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
    public async Task ToByteArray_ShouldReturnCopy_WhenContentIsOwnedBuffer()
    {
        byte[] buffer = [0x01, 0x02, 0x03];
        RawMessage message = new(new OwnedReadOnlyMemoryStream(buffer));

        message.ToByteArray().ShouldNotBeSameAs(buffer);
        (await message.ToByteArrayAsync()).ShouldNotBeSameAs(buffer);
        ((byte[]?)message).ShouldNotBeSameAs(buffer);
    }

    [Fact]
    public async Task ToByteArray_ShouldReturnRemainingBytes_WhenOwnedContentIsPartiallyRead()
    {
        byte[] buffer = [0x01, 0x02, 0x03];
        RawMessage message = new(new OwnedReadOnlyMemoryStream(buffer) { Position = 1 });

        byte[]? syncResult = message.ToByteArray();
        byte[]? asyncResult = await message.ToByteArrayAsync();

        syncResult.ShouldBe([0x02, 0x03]);
        asyncResult.ShouldBe([0x02, 0x03]);
    }

    [Fact]
    public async Task ToByteArray_ShouldNotExposeOwnedBuffer_WhenReturnedArrayIsModified()
    {
        byte[] buffer = [0x01, 0x02, 0x03];
        RawMessage message = new(new OwnedReadOnlyMemoryStream(buffer));

        byte[]? syncResult = message.ToByteArray();
        syncResult![0] = 0xFF;
        byte[]? asyncResult = await message.ToByteArrayAsync();
        asyncResult![1] = 0xFF;
        byte[]? implicitResult = message;
        implicitResult![2] = 0xFF;

        buffer.ShouldBe([0x01, 0x02, 0x03]);
        message.Content.ReadAll().ShouldBe([0x01, 0x02, 0x03]);
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
