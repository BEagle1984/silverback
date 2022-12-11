// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Reflection;

namespace Silverback.Tests;

public class ResourcesHelper
{
    private readonly Assembly _assembly;

    public ResourcesHelper(Assembly assembly)
    {
        _assembly = assembly;
    }

    public string GetAsString(string resourceName)
    {
        using Stream? resource = _assembly.GetManifestResourceStream(resourceName);

        using StreamReader reader = new(resource ?? throw new InvalidOperationException());

        return reader.ReadToEnd();
    }
}
