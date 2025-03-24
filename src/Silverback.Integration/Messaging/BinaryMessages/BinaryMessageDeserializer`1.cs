// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryMessages;

/// <summary>
///     Wraps the consumed bytes stream into an instance of <typeparamref name="TModel" />.
/// </summary>
/// <typeparam name="TModel">
///     The type of the <see cref="IBinaryMessage" /> implementation.
/// </typeparam>
public sealed class BinaryMessageDeserializer<TModel> : IBinaryMessageDeserializer, IEquatable<BinaryMessageDeserializer<TModel>>
    where TModel : IBinaryMessage, new()
{
    private readonly Type _type = typeof(TModel);

    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders => false;

    /// <inheritdoc cref="IMessageDeserializer.DeserializeAsync" />
    public ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        Type type = SerializationHelper.GetTypeFromHeaders(headers, _type);

        TModel binaryMessage = type == _type ? new TModel() : (TModel)Activator.CreateInstance(type)!;
        binaryMessage.Content = messageStream;

        return ValueTask.FromResult(new DeserializedMessage(binaryMessage, type));
    }

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public IMessageSerializer GetCompatibleSerializer() => new BinaryMessageSerializer();

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(BinaryMessageDeserializer<TModel>? other) => true;

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((BinaryMessageDeserializer<TModel>)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => 42;
}
