// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class StringMessageFixture
{
    [Fact]
    public void ImplicitOperator_ShouldConvertToString()
    {
        StringMessage message1 = new("test");
        StringMessage<TestEventOne> message2 = new("test");

        string? content1 = message1;
        string? content2 = message2;

        content1.ShouldBe(message1.Content);
        content2.ShouldBe(message2.Content);
    }

    [Fact]
    public void ImplicitOperator_ShouldConvertFromString()
    {
        StringMessage message1 = "test";
        StringMessage<TestEventOne> message2 = "test";

        message1.Content.ShouldBe("test");
        message2.Content.ShouldBe("test");
    }

    [Fact]
    public void FromString_ShouldCreateMessage()
    {
        StringMessage message1 = StringMessage.FromString("test");
        StringMessage<TestEventOne> message2 = StringMessage<TestEventOne>.FromString("test");

        message1.Content.ShouldBe("test");
        message2.Content.ShouldBe("test");
    }

    [Fact]
    public void ToString_ShouldReturnContent()
    {
        StringMessage message1 = new("test");
        StringMessage<TestEventOne> message2 = new("test");

        string? content1 = message1.ToString();
        string? content2 = message2.ToString();

        content1.ShouldBe(message1.Content);
        content2.ShouldBe(message2.Content);
    }

    [Fact]
    public void ToString_ShouldReturnNull_WhenContentIsNull()
    {
        StringMessage message1 = new(null);
        StringMessage<TestEventOne> message2 = new(null);

        string? content1 = message1.ToString();
        string? content2 = message2.ToString();

        content1.ShouldBeNull();
        content2.ShouldBeNull();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameContent()
    {
        StringMessage message1 = new("test");
        StringMessage message2 = new("test");

        bool result = message1.Equals(message2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameTypeAndContent()
    {
        StringMessage<TestEventOne> message1 = new("test");
        StringMessage<TestEventOne> message2 = new("test");

        bool result = message1.Equals(message2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentContent()
    {
        StringMessage message1 = new("test1");
        StringMessage message2 = new("test2");

        bool result = message1.Equals(message2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        StringMessage<TestEventOne> message1 = new("test");
        StringMessage<TestEventTwo> message2 = new("test");

        bool result = message1.Equals(message2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenSameTypeButDifferentContent()
    {
        StringMessage<TestEventOne> message1 = new("test1");
        StringMessage<TestEventOne> message2 = new("test2");

        bool result = message1.Equals(message2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_ShouldReturnTrue_WhenSameContent()
    {
        StringMessage message1 = new("test");
        StringMessage message2 = new("test");

        bool result = message1 == message2;

        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_ShouldReturnFalse_WhenDifferentContent()
    {
        StringMessage message1 = new("test1");
        StringMessage message2 = new("test2");

        bool result = message1 == message2;

        result.ShouldBeFalse();
    }

    [Fact]
    public void InequalityOperator_ShouldReturnFalse_WhenSameContent()
    {
        StringMessage message1 = new("test");
        StringMessage message2 = new("test");

        bool result = message1 != message2;

        result.ShouldBeFalse();
    }

    [Fact]
    public void InequalityOperator_ShouldReturnTrue_WhenDifferentContent()
    {
        StringMessage message1 = new("test1");
        StringMessage message2 = new("test2");

        bool result = message1 != message2;

        result.ShouldBeTrue();
    }
}
