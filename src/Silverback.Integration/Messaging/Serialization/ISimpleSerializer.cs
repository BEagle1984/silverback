// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Serialization;

public interface ISimpleSerializer
{
    byte[]? Serialize(object? value);
}
