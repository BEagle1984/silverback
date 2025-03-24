// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Headers;

internal sealed class CustomHeadersMappings : ICustomHeadersMappings
{
    private Dictionary<string, string>? _mappings;

    private Dictionary<string, string>? _inverseMappings;

    public int Count { get; private set; }

    public void Add(string defaultHeaderName, string customHeaderName)
    {
        _mappings ??= [];

        _mappings[defaultHeaderName] = customHeaderName;

        _inverseMappings = _mappings.ToDictionary(pair => pair.Value, pair => pair.Key);

        Count = _mappings.Count;
    }

    public void Apply(MessageHeaderCollection headers) => Map(headers, _mappings);

    public void Revert(MessageHeaderCollection headers) => Map(headers, _inverseMappings);

    private static void Map(MessageHeaderCollection headers, Dictionary<string, string>? mappings)
    {
        Check.NotNull(headers, nameof(headers));

        if (mappings == null)
            return;

        for (int i = 0; i < headers.Count; i++)
        {
            MessageHeader header = headers[i];

            if (mappings.TryGetValue(header.Name, out string? mappedName))
                headers[i] = header with { Name = mappedName };
        }
    }
}
