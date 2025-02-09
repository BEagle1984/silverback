// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.Kafka.SchemaRegistry.TestTypes;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class ProtobufMessageTypeValidatorFixture
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenMessageTypeIsValidProtobufMessage()
    {
        Action act = () => ProtobufMessageTypeValidator.Validate(typeof(ProtobufMessage));

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMessageTypeIsNotIMessage()
    {
        Action act = () => ProtobufMessageTypeValidator.Validate(typeof(TestEventOne));

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("TestEventOne does not implement IMessage<TestEventOne>.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMessageIMessageTypeNotCorrect()
    {
        Action act = () => ProtobufMessageTypeValidator.Validate(typeof(ProtobufMessageWrongType));

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("ProtobufMessageWrongType does not implement IMessage<ProtobufMessageWrongType>.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMessageTypeDoesNotHaveParameterlessConstructor()
    {
        Action act = () => ProtobufMessageTypeValidator.Validate(typeof(ProtobufMessageNoCtor));

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("ProtobufMessageNoCtor does not have a public parameterless constructor.");
    }

    [SuppressMessage("Design", "CA1067:Override Object.Equals(object) when implementing IEquatable<T>", Justification = "Test class")]
    private class ProtobufMessageNoCtor : IMessage<ProtobufMessageNoCtor>
    {
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Test code")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Test code")]
        public ProtobufMessageNoCtor(string param)
        {
        }

        public MessageDescriptor Descriptor { get; } = null!;

        public void MergeFrom(ProtobufMessageNoCtor message) => throw new NotSupportedException();

        public void MergeFrom(CodedInputStream input) => throw new NotSupportedException();

        public void WriteTo(CodedOutputStream output) => throw new NotSupportedException();

        public int CalculateSize() => throw new NotSupportedException();

        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Test class")]
        public bool Equals(ProtobufMessageNoCtor? other) => throw new NotSupportedException();

        public ProtobufMessageNoCtor Clone() => throw new NotSupportedException();
    }

    [SuppressMessage("Design", "CA1067:Override Object.Equals(object) when implementing IEquatable<T>", Justification = "Test class")]
    private class ProtobufMessageWrongType : IMessage<ProtobufMessage>
    {
        public MessageDescriptor Descriptor { get; } = null!;

        public void MergeFrom(ProtobufMessage message) => throw new NotSupportedException();

        public void MergeFrom(CodedInputStream input) => throw new NotSupportedException();

        public void WriteTo(CodedOutputStream output) => throw new NotSupportedException();

        public int CalculateSize() => throw new NotSupportedException();

        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Test class")]
        public bool Equals(ProtobufMessage? other) => throw new NotSupportedException();

        public ProtobufMessage Clone() => throw new NotSupportedException();
    }
}
