// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.TestTypes;

[SuppressMessage("Design", "CA1067:Override Object.Equals(object) when implementing IEquatable<T>", Justification = "Test class")]
public class ProtobufMessage : IMessage<ProtobufMessage>
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
