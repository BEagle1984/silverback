// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Silverback.Messaging.Validation;

internal static class MessageValidator
{
    private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> NestedObjectPropertiesCache = new();

    public static bool IsValid(
        object message,
        MessageValidationMode validationMode,
        [NotNullWhen(false)] out string? validationErrors)
    {
        bool validMessage = TryValidateObject(message, out IReadOnlyList<ValidationResult> results);

        if (validMessage)
        {
            validationErrors = null;
            return true;
        }

        validationErrors = string.Join(
            string.Empty,
            results.Select(validationResult => $"{Environment.NewLine}- {validationResult.ErrorMessage}"));

        if (validationMode == MessageValidationMode.ThrowException)
            throw new MessageValidationException($"The message is not valid: {validationErrors}");

        return false;
    }

    private static bool TryValidateObject(object obj, out IReadOnlyList<ValidationResult> validationResults)
    {
        ValidationContext validationContext = new(obj);

        List<ValidationResult> results = [];
        validationResults = results;
        bool result = Validator.TryValidateObject(obj, validationContext, results, true);

        return result & ValidateNestedObjects(obj, results);
    }

    private static bool ValidateNestedObjects(object obj, List<ValidationResult> results)
    {
        bool result = true;

        foreach (PropertyInfo property in GetNestedObjectProperties(obj))
        {
            object? value = property.GetValue(obj);

            if (value == null)
                continue;

            if (value is IEnumerable<object> valueEnumerable)
            {
                foreach (object? valueItem in valueEnumerable)
                {
                    result = ValidateNestedObject(valueItem, results);
                }
            }
            else
            {
                result = ValidateNestedObject(value, results);
            }
        }

        return result;
    }

    private static bool ValidateNestedObject(object obj, List<ValidationResult> results)
    {
        if (TryValidateObject(obj, out IReadOnlyList<ValidationResult> nestedResults))
            return true;

        results.AddRange(nestedResults);
        return false;
    }

    private static List<PropertyInfo> GetNestedObjectProperties(object obj)
    {
        Type type = obj.GetType();

        return NestedObjectPropertiesCache.GetOrAdd(
            type,
            static addedType => GetNestedObjectProperties(addedType));
    }

    private static List<PropertyInfo> GetNestedObjectProperties(Type type)
    {
        if (type.IsPrimitive || type.IsArray && type.HasElementType && type.GetElementType()!.IsPrimitive)
            return [];

        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
            .Where(propertyInfo => NestedTypeMustBeValidated(propertyInfo.PropertyType))
            .ToList();
    }

    private static bool NestedTypeMustBeValidated(Type type)
    {
        if (type == typeof(string) || type.IsValueType || type == typeof(Type))
            return false;

        if (type.IsArray && type.HasElementType)
        {
            Type elementType = type.GetElementType()!;
            return NestedTypeMustBeValidated(elementType);
        }

        return true;
    }
}
