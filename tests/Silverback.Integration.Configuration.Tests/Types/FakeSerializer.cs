// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.Configuration.Types
{
    public class FakeSerializer : IMessageSerializer
    {
        public FakeSerializerSettings Settings { get; set; } = new FakeSerializerSettings();

        public byte[]? Serialize(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) => throw new System.NotImplementedException();

        public (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) => throw new System.NotImplementedException();

        public Task<byte[]?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) => throw new System.NotImplementedException();

        public Task<(object?, Type)> DeserializeAsync(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) => throw new System.NotImplementedException();
    }
}