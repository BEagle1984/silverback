// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback;

internal class SilverbackContext : ISilverbackContext
{
    private readonly Dictionary<Guid, object> _objects = [];

    public SilverbackContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public void AddObject(Guid objectTypeId, object obj)
    {
        if (!_objects.TryAdd(objectTypeId, obj) && GetObject(objectTypeId) != obj)
            throw new InvalidOperationException($"An object of type {objectTypeId} has already been added.");
    }

    public void SetObject(Guid objectTypeId, object obj) => _objects[objectTypeId] = obj;

    public bool RemoveObject(Guid objectTypeId) => _objects.Remove(objectTypeId);

    public T GetObject<T>(Guid objectTypeId) => TryGetObject(objectTypeId, out T? obj)
        ? obj
        : throw new InvalidOperationException($"The object with type id {objectTypeId} was not found.");

    public object GetObject(Guid objectTypeId) => TryGetObject(objectTypeId, out object? obj)
        ? obj
        : throw new InvalidOperationException($"The object with type id {objectTypeId} was not found.");

    public bool TryGetObject<T>(Guid objectTypeId, [NotNullWhen(true)] out T? obj)
    {
        if (TryGetObject(objectTypeId, out object? tmpObj))
        {
            if (tmpObj is not T typedObj)
                throw new InvalidOperationException($"The object with type id {objectTypeId} is not of type {typeof(T)}.");

            obj = typedObj;
            return true;
        }

        obj = default;
        return false;
    }

    public bool TryGetObject(Guid objectTypeId, [NotNullWhen(true)] out object? obj) => _objects.TryGetValue(objectTypeId, out obj);

    public T GetOrAddObject<T>(Guid objectTypeId, Func<T> factory)
    {
        Check.NotNull(factory, nameof(factory));

        if (TryGetObject(objectTypeId, out T? obj))
            return obj;

        T newObj = factory.Invoke() ?? throw new InvalidOperationException("The factory returned null.");
        AddObject(objectTypeId, newObj);
        return newObj;
    }

    public TObject GetOrAddObject<TObject, TArg>(Guid objectTypeId, Func<TArg, TObject> factory, TArg argument)
    {
        Check.NotNull(factory, nameof(factory));

        if (TryGetObject(objectTypeId, out TObject? obj))
            return obj;

        TObject newObj = factory.Invoke(argument) ?? throw new InvalidOperationException("The factory returned null.");
        AddObject(objectTypeId, newObj);
        return newObj;
    }
}
